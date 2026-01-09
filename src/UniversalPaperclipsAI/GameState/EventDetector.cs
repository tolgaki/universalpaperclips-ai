using Microsoft.Extensions.Logging;

namespace UniversalPaperclipsAI.GameState;

/// <summary>
/// Detects significant game events that should trigger an LLM decision.
/// </summary>
public sealed class EventDetector
{
    private GameStateSnapshot? _previousState;
    private DateTime _lastDecisionTime = DateTime.MinValue;
    private readonly int _decisionIntervalMs;
    private readonly ILogger<EventDetector>? _logger;

    /// <summary>
    /// Initializes a new instance of the EventDetector.
    /// </summary>
    /// <param name="decisionIntervalMs">Minimum interval between scheduled decisions.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public EventDetector(int decisionIntervalMs = 3000, ILogger<EventDetector>? logger = null)
    {
        _decisionIntervalMs = decisionIntervalMs;
        _logger = logger;
    }

    /// <summary>
    /// Determines if a decision should be triggered based on the current game state.
    /// Updates internal state if a decision is triggered.
    /// </summary>
    /// <param name="currentState">Current game state.</param>
    /// <returns>True if a decision should be triggered.</returns>
    public bool ShouldTriggerDecision(GameStateSnapshot currentState)
    {
        var (shouldTrigger, reasons) = EvaluateTriggers(currentState);

        if (shouldTrigger)
        {
            _logger?.LogDebug("Decision triggered: {Reasons}", string.Join(", ", reasons));
            _previousState = currentState;
            _lastDecisionTime = DateTime.UtcNow;
        }

        return shouldTrigger;
    }

    /// <summary>
    /// Gets the reasons why a decision would be triggered for the current state.
    /// Does not update internal state.
    /// </summary>
    /// <param name="currentState">Current game state.</param>
    /// <returns>List of trigger reasons.</returns>
    public List<string> GetTriggerReasons(GameStateSnapshot currentState)
    {
        var (_, reasons) = EvaluateTriggers(currentState);
        return reasons;
    }

    /// <summary>
    /// Evaluates all trigger conditions and returns both the result and reasons.
    /// This is the single source of truth for trigger logic.
    /// </summary>
    private (bool ShouldTrigger, List<string> Reasons) EvaluateTriggers(GameStateSnapshot currentState)
    {
        var reasons = new List<string>();

        // Always trigger on first state
        if (_previousState == null)
        {
            reasons.Add("Initial state");
            return (true, reasons);
        }

        // Check for significant events
        if (HasNewProjectsAvailable(currentState))
            reasons.Add("New project available");

        if (HasPhaseChanged(currentState))
            reasons.Add($"Phase changed to {currentState.Phase}");

        if (HasSignificantResourceChange(currentState))
            reasons.Add("Significant resource change");

        if (HasNewActionAvailable(currentState))
            reasons.Add("New action became available");

        // Fallback: trigger every N seconds regardless
        if ((DateTime.UtcNow - _lastDecisionTime).TotalMilliseconds >= _decisionIntervalMs)
            reasons.Add("Scheduled interval");

        return (reasons.Count > 0, reasons);
    }

    private bool HasNewProjectsAvailable(GameStateSnapshot current)
    {
        if (_previousState == null) return current.Projects.Available.Any();

        var prevIds = _previousState.Projects.Available.Select(p => p.Id).ToHashSet();
        return current.Projects.Available.Any(p => !prevIds.Contains(p.Id));
    }

    private bool HasPhaseChanged(GameStateSnapshot current) =>
        _previousState != null && current.Phase != _previousState.Phase;

    private bool HasSignificantResourceChange(GameStateSnapshot current)
    {
        if (_previousState == null) return false;

        // Trust change is always significant
        if (current.Resources.Trust != _previousState.Resources.Trust)
            return true;

        // Wire running low
        if (current.Resources.Wire < 100 && _previousState.Resources.Wire >= 100)
            return true;

        // Money threshold crossed
        if (CrossedThreshold(current.Resources.Funds, _previousState.Resources.Funds, 100, 1000, 10000, 100000))
            return true;

        // Paperclips threshold
        if (CrossedThreshold(current.Resources.Paperclips, _previousState.Resources.Paperclips,
            100, 1000, 10000, 100000, 1000000))
            return true;

        return false;
    }

    private bool HasNewActionAvailable(GameStateSnapshot current)
    {
        if (_previousState == null) return false;

        var prevActions = _previousState.AvailableActions.ToHashSet();
        return current.AvailableActions.Any(a => !prevActions.Contains(a));
    }

    private static bool CrossedThreshold(double current, double previous, params double[] thresholds)
    {
        foreach (var threshold in thresholds)
        {
            if ((previous < threshold && current >= threshold) ||
                (previous >= threshold && current < threshold))
                return true;
        }
        return false;
    }

    private static bool CrossedThreshold(long current, long previous, params long[] thresholds)
    {
        foreach (var threshold in thresholds)
        {
            if ((previous < threshold && current >= threshold) ||
                (previous >= threshold && current < threshold))
                return true;
        }
        return false;
    }
}
