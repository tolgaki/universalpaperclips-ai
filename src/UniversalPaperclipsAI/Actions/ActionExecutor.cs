using Microsoft.Extensions.Logging;
using UniversalPaperclipsAI.Browser;

namespace UniversalPaperclipsAI.Actions;

/// <summary>
/// Executes game actions through browser automation.
/// </summary>
public sealed class ActionExecutor
{
    private const int MaxHistorySize = 500;
    private const int DefaultActionDelayMs = 50;
    private const int RapidClickDelayMs = 10;
    private const int DefaultMaxActionsPerDecision = 5;

    private readonly BrowserController _browser;
    private readonly int _maxActionsPerDecision;
    private readonly LinkedList<ActionLogEntry> _actionHistory = new();
    private readonly object _historyLock = new();
    private readonly ILogger<ActionExecutor>? _logger;

    /// <summary>
    /// Gets the action history (limited to last <see cref="MaxHistorySize"/> entries).
    /// Returns a snapshot copy for thread safety.
    /// </summary>
    public IReadOnlyList<ActionLogEntry> ActionHistory
    {
        get
        {
            lock (_historyLock)
            {
                return _actionHistory.ToList();
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the ActionExecutor.
    /// </summary>
    /// <param name="browser">Browser controller for DOM interactions.</param>
    /// <param name="maxActionsPerDecision">Maximum number of actions to execute per decision batch.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public ActionExecutor(BrowserController browser, int maxActionsPerDecision = DefaultMaxActionsPerDecision, ILogger<ActionExecutor>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(browser);
        _browser = browser;
        _maxActionsPerDecision = maxActionsPerDecision > 0 ? maxActionsPerDecision : DefaultMaxActionsPerDecision;
        _logger = logger;
    }

    /// <summary>
    /// Executes a single game action.
    /// </summary>
    /// <param name="actionName">Name of the action to execute.</param>
    /// <param name="parameters">Optional parameters for the action.</param>
    /// <returns>Result indicating success or failure.</returns>
    public async Task<ActionResult> ExecuteAsync(string actionName, Dictionary<string, object>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(actionName))
        {
            return new ActionResult(false, "Action name cannot be null or empty");
        }

        var logEntry = new ActionLogEntry
        {
            Timestamp = DateTime.UtcNow,
            ActionName = actionName,
            Parameters = parameters ?? new()
        };

        try
        {
            // Handle project activation specially
            if (actionName.StartsWith("ActivateProject:"))
            {
                var projectId = actionName.Replace("ActivateProject:", "");
                return await ExecuteProjectActivationAsync(projectId, logEntry);
            }

            // Look up standard action
            var action = GameActions.Get(actionName);
            if (action == null)
            {
                logEntry.Success = false;
                logEntry.Message = $"Unknown action: {actionName}";
                AddToHistory(logEntry);
                return new ActionResult(false, logEntry.Message);
            }

            // Execute the action
            if (action.IsSlider)
            {
                await _browser.SetSliderAsync(action.Selector, action.SliderValue);
                logEntry.Success = true;
                logEntry.Message = $"Set slider to {action.SliderValue}";
            }
            else
            {
                for (int i = 0; i < action.ClickCount; i++)
                {
                    await _browser.ClickIfEnabledAsync(action.Selector);
                    if (action.ClickCount > 1)
                        await Task.Delay(RapidClickDelayMs);
                }
                logEntry.Success = true;
                logEntry.Message = action.ClickCount > 1
                    ? $"Clicked {action.ClickCount} times"
                    : "Clicked";
            }

            AddToHistory(logEntry);
            return new ActionResult(true, logEntry.Message);
        }
        catch (Exception ex)
        {
            logEntry.Success = false;
            logEntry.Message = $"Error: {ex.Message}";
            _logger?.LogWarning(ex, "Action {ActionName} failed", actionName);
            AddToHistory(logEntry);
            return new ActionResult(false, logEntry.Message);
        }
    }

    private void AddToHistory(ActionLogEntry entry)
    {
        // Thread-safe history management with O(1) pruning
        lock (_historyLock)
        {
            _actionHistory.AddLast(entry);

            // Prune history to prevent memory leak
            while (_actionHistory.Count > MaxHistorySize)
            {
                _actionHistory.RemoveFirst();
            }
        }
    }

    private async Task<ActionResult> ExecuteProjectActivationAsync(string projectId, ActionLogEntry logEntry)
    {
        try
        {
            // Projects have dynamic IDs like "project1", "project2", etc.
            var selector = $"#{projectId}";
            await _browser.ClickIfEnabledAsync(selector);

            logEntry.Success = true;
            logEntry.Message = $"Activated project: {projectId}";
            AddToHistory(logEntry);
            return new ActionResult(true, logEntry.Message);
        }
        catch (Exception ex)
        {
            logEntry.Success = false;
            logEntry.Message = $"Failed to activate project: {ex.Message}";
            _logger?.LogWarning(ex, "Project activation failed for {ProjectId}", projectId);
            AddToHistory(logEntry);
            return new ActionResult(false, logEntry.Message);
        }
    }

    /// <summary>
    /// Executes a batch of actions sequentially, limited to MaxActionsPerDecision.
    /// Actions are validated against the available actions list if provided.
    /// </summary>
    /// <param name="actions">Actions to execute.</param>
    /// <param name="availableActions">Optional list of available actions to validate against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of actions successfully executed.</returns>
    public async Task<int> ExecuteBatchAsync(
        IEnumerable<LLMAction> actions,
        IReadOnlyList<string>? availableActions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actions);

        // Enforce maximum actions per decision to prevent runaway LLM responses
        var limitedActions = actions.Take(_maxActionsPerDecision);
        var executedCount = 0;

        foreach (var action in limitedActions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Validate action against available actions if provided
            if (availableActions != null && !IsActionValid(action.Type, availableActions))
            {
                _logger?.LogWarning("Skipping invalid action '{ActionType}' - not in available actions list", action.Type);

                var logEntry = new ActionLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    ActionName = action.Type,
                    Parameters = action.Parameters,
                    Success = false,
                    Message = "Action not available in current game state"
                };
                AddToHistory(logEntry);
                continue;
            }

            var result = await ExecuteAsync(action.Type, action.Parameters);
            if (result.Success)
            {
                executedCount++;
            }
            await Task.Delay(DefaultActionDelayMs, cancellationToken);
        }

        return executedCount;
    }

    private static readonly HashSet<string> AlwaysValidActions = new()
    {
        "SetLowRisk", "SetMediumRisk", "SetHighRisk",
        "ToggleAutoWire", "ToggleAutoTourney"
    };

    /// <summary>
    /// Validates whether an action is in the available actions list.
    /// </summary>
    private static bool IsActionValid(string actionType, IReadOnlyList<string> availableActions)
    {
        if (string.IsNullOrWhiteSpace(actionType))
            return false;

        // Direct match
        if (availableActions.Contains(actionType))
            return true;

        // Handle project actions (ActivateProject:projectN matches ActivateProject:projectN in available)
        if (actionType.StartsWith("ActivateProject:"))
            return availableActions.Contains(actionType);

        // Handle multi-click variants (MakePaperclip10 should be valid if MakePaperclip is available)
        if (actionType == "MakePaperclip10" && availableActions.Contains("MakePaperclip"))
            return true;

        // Known actions that may not be in the dynamic available list but are always valid if the button exists
        if (AlwaysValidActions.Contains(actionType))
            return true;

        return false;
    }

    public IEnumerable<ActionLogEntry> GetRecentHistory(int count = 10)
    {
        lock (_historyLock)
        {
            return _actionHistory.TakeLast(count).ToList();
        }
    }

    public void ClearHistory()
    {
        lock (_historyLock)
        {
            _actionHistory.Clear();
        }
    }
}

public class ActionLogEntry
{
    public DateTime Timestamp { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] {ActionName}: {(Success ? "OK" : "FAIL")} - {Message}";
}

public class LLMAction
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}
