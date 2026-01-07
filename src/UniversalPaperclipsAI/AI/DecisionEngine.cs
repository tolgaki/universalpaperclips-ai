using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using UniversalPaperclipsAI.Actions;
using UniversalPaperclipsAI.Core;
using UniversalPaperclipsAI.GameState;

namespace UniversalPaperclipsAI.AI;

/// <summary>
/// Orchestrates LLM decision-making for game actions.
/// </summary>
public sealed class DecisionEngine
{
    private const int MaxHistorySize = 100;
    private const int MaxRetryAttempts = 3;
    private const int RetryDelayMs = 1000;

    private readonly OpenAISettings _settings;
    private readonly PromptBuilder _promptBuilder;
    private readonly ChatClient _client;
    private readonly List<DecisionLogEntry> _decisionHistory = new();
    private readonly ILogger<DecisionEngine>? _logger;

    /// <summary>
    /// Gets the decision history (limited to last <see cref="MaxHistorySize"/> entries).
    /// </summary>
    public IReadOnlyList<DecisionLogEntry> DecisionHistory => _decisionHistory;

    /// <summary>
    /// Gets the most recent decision, if any.
    /// </summary>
    public DecisionLogEntry? LastDecision => _decisionHistory.LastOrDefault();

    /// <summary>
    /// Initializes a new instance of the DecisionEngine.
    /// </summary>
    /// <param name="settings">OpenAI configuration settings.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when API key is not configured.</exception>
    public DecisionEngine(OpenAISettings settings, ILogger<DecisionEngine>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;
        _logger = logger;
        _promptBuilder = new PromptBuilder();

        var apiKey = !string.IsNullOrEmpty(settings.ApiKey)
            ? settings.ApiKey
            : Environment.GetEnvironmentVariable("OPENAI_API_KEY")
              ?? throw new InvalidOperationException("OpenAI API key not configured. Set OPENAI_API_KEY environment variable or configure in appsettings.json");

        var openAiClient = new OpenAIClient(apiKey);
        _client = openAiClient.GetChatClient(settings.Model);
    }

    /// <summary>
    /// Makes a decision based on the current game state.
    /// </summary>
    /// <param name="state">Current game state snapshot.</param>
    /// <param name="recentActions">Recent actions taken for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The LLM's decision with reasoning and actions.</returns>
    public async Task<LLMDecision> MakeDecisionAsync(
        GameStateSnapshot state,
        IEnumerable<ActionLogEntry> recentActions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(recentActions);

        var userPrompt = _promptBuilder.BuildDecisionPrompt(state, recentActions);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(SystemPrompt.Content),
            new UserChatMessage(userPrompt)
        };

        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = _settings.MaxTokens,
            Temperature = 0.7f
        };

        // Retry logic with exponential backoff
        Exception? lastException = null;
        for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var response = await _client.CompleteChatAsync(messages, options, cancellationToken);
                var content = response.Value.Content[0].Text;

                // Parse the JSON response
                var decision = ParseResponse(content);

                // Log the decision and prune history if needed
                var logEntry = new DecisionLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    GamePhase = state.Phase,
                    StateSnapshot = CreateStateSnapshotSummary(state),
                    Reasoning = decision.Reasoning,
                    Actions = decision.Actions,
                    Priority = decision.Priority,
                    RawResponse = content
                };

                _decisionHistory.Add(logEntry);

                // Prune history to prevent memory leak (Issue #1)
                while (_decisionHistory.Count > MaxHistorySize)
                {
                    _decisionHistory.RemoveAt(0);
                }

                return decision;
            }
            catch (OperationCanceledException)
            {
                throw; // Don't retry on cancellation
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger?.LogWarning(ex, "Decision attempt {Attempt}/{MaxAttempts} failed", attempt, MaxRetryAttempts);

                if (attempt < MaxRetryAttempts)
                {
                    var delay = RetryDelayMs * (int)Math.Pow(2, attempt - 1); // Exponential backoff
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        _logger?.LogError(lastException, "All {MaxAttempts} decision attempts failed", MaxRetryAttempts);

        // Return a safe default decision after all retries exhausted
        return new LLMDecision
        {
            Reasoning = $"Error after {MaxRetryAttempts} attempts: {lastException?.Message}. Taking safe default action.",
            Actions = GetSafeDefaultActions(state),
            Priority = "Recovery from error"
        };
    }

    private LLMDecision ParseResponse(string content)
    {
        try
        {
            // Clean up the response - remove markdown code blocks if present
            content = content.Trim();
            if (content.StartsWith("```json"))
                content = content[7..];
            if (content.StartsWith("```"))
                content = content[3..];
            if (content.EndsWith("```"))
                content = content[..^3];
            content = content.Trim();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var decision = JsonSerializer.Deserialize<LLMDecision>(content, options);
            return decision ?? new LLMDecision
            {
                Reasoning = "Failed to parse response",
                Actions = new List<LLMAction>(),
                Priority = "Error recovery"
            };
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Failed to parse LLM response: {ex.Message}");
            Console.WriteLine($"Raw response: {content}");

            return new LLMDecision
            {
                Reasoning = "Failed to parse LLM response as JSON",
                Actions = new List<LLMAction>(),
                Priority = "Error recovery"
            };
        }
    }

    private List<LLMAction> GetSafeDefaultActions(GameStateSnapshot state)
    {
        var actions = new List<LLMAction>();

        // In early game, make paperclips
        if (state.Phase == GamePhase.Early && state.AvailableActions.Contains("MakePaperclip"))
        {
            actions.Add(new LLMAction { Type = "MakePaperclip10" });
        }

        // Always buy wire if running low
        if (state.Resources.Wire < 500 && state.AvailableActions.Contains("BuyWire"))
        {
            actions.Add(new LLMAction { Type = "BuyWire" });
        }

        return actions;
    }

    private string CreateStateSnapshotSummary(GameStateSnapshot state)
    {
        return $"Phase: {state.Phase}, Clips: {state.Resources.Paperclips:N0}, " +
               $"Funds: ${state.Resources.Funds:N2}, Wire: {state.Resources.Wire:N0}";
    }

    public IEnumerable<DecisionLogEntry> GetRecentDecisions(int count = 5) =>
        _decisionHistory.TakeLast(count);
}

public class LLMDecision
{
    public string Reasoning { get; set; } = string.Empty;
    public List<LLMAction> Actions { get; set; } = new();
    public string Priority { get; set; } = string.Empty;
}

public class DecisionLogEntry
{
    public DateTime Timestamp { get; set; }
    public GamePhase GamePhase { get; set; }
    public string StateSnapshot { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public List<LLMAction> Actions { get; set; } = new();
    public string Priority { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;
}
