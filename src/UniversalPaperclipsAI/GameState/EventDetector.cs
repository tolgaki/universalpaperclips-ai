namespace UniversalPaperclipsAI.GameState;

/// <summary>
/// Detects significant game events that should trigger an LLM decision.
/// </summary>
public sealed class EventDetector
{
    private GameStateSnapshot? _previousState;
    private DateTime _lastDecisionTime = DateTime.MinValue;
    private readonly int _decisionIntervalMs;

    public EventDetector(int decisionIntervalMs = 3000)
    {
        _decisionIntervalMs = decisionIntervalMs;
    }

    public bool ShouldTriggerDecision(GameStateSnapshot currentState)
    {
        var shouldTrigger = false;
        var reasons = new List<string>();

        // Always trigger on first state
        if (_previousState == null)
        {
            _previousState = currentState;
            _lastDecisionTime = DateTime.UtcNow;
            return true;
        }

        // Check for significant events
        if (HasNewProjectsAvailable(currentState))
        {
            reasons.Add("New project available");
            shouldTrigger = true;
        }

        if (HasPhaseChanged(currentState))
        {
            reasons.Add($"Phase changed to {currentState.Phase}");
            shouldTrigger = true;
        }

        if (HasSignificantResourceChange(currentState))
        {
            reasons.Add("Significant resource change");
            shouldTrigger = true;
        }

        if (HasNewActionAvailable(currentState))
        {
            reasons.Add("New action became available");
            shouldTrigger = true;
        }

        // Fallback: trigger every N seconds regardless
        if ((DateTime.UtcNow - _lastDecisionTime).TotalMilliseconds >= _decisionIntervalMs)
        {
            reasons.Add("Scheduled interval");
            shouldTrigger = true;
        }

        if (shouldTrigger)
        {
            _previousState = currentState;
            _lastDecisionTime = DateTime.UtcNow;
        }

        return shouldTrigger;
    }

    public List<string> GetTriggerReasons(GameStateSnapshot currentState)
    {
        var reasons = new List<string>();

        if (_previousState == null)
        {
            reasons.Add("Initial state");
            return reasons;
        }

        if (HasNewProjectsAvailable(currentState))
            reasons.Add("New project available");

        if (HasPhaseChanged(currentState))
            reasons.Add($"Phase changed to {currentState.Phase}");

        if (HasSignificantResourceChange(currentState))
            reasons.Add("Significant resource change");

        if (HasNewActionAvailable(currentState))
            reasons.Add("New action became available");

        if ((DateTime.UtcNow - _lastDecisionTime).TotalMilliseconds >= _decisionIntervalMs)
            reasons.Add("Scheduled interval");

        return reasons;
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
