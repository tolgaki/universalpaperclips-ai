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

    private readonly BrowserController _browser;
    private readonly List<ActionLogEntry> _actionHistory = new();
    private readonly ILogger<ActionExecutor>? _logger;

    /// <summary>
    /// Gets the action history (limited to last <see cref="MaxHistorySize"/> entries).
    /// </summary>
    public IReadOnlyList<ActionLogEntry> ActionHistory => _actionHistory;

    /// <summary>
    /// Initializes a new instance of the ActionExecutor.
    /// </summary>
    /// <param name="browser">Browser controller for DOM interactions.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public ActionExecutor(BrowserController browser, ILogger<ActionExecutor>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(browser);
        _browser = browser;
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
                _actionHistory.Add(logEntry);
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
        _actionHistory.Add(entry);

        // Prune history to prevent memory leak (Issue #2)
        while (_actionHistory.Count > MaxHistorySize)
        {
            _actionHistory.RemoveAt(0);
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
    /// Executes a batch of actions sequentially.
    /// </summary>
    /// <param name="actions">Actions to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ExecuteBatchAsync(IEnumerable<LLMAction> actions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actions);

        foreach (var action in actions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ExecuteAsync(action.Type, action.Parameters);
            await Task.Delay(DefaultActionDelayMs, cancellationToken);
        }
    }

    public IEnumerable<ActionLogEntry> GetRecentHistory(int count = 10) =>
        _actionHistory.TakeLast(count);

    public void ClearHistory() => _actionHistory.Clear();
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
