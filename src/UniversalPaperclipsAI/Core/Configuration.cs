namespace UniversalPaperclipsAI.Core;

/// <summary>
/// Root configuration for the application.
/// </summary>
public sealed class AppConfiguration
{
    /// <summary>OpenAI API settings.</summary>
    public OpenAISettings OpenAI { get; set; } = new();

    /// <summary>Game-related settings.</summary>
    public GameSettings Game { get; set; } = new();

    /// <summary>Display and UI settings.</summary>
    public DisplaySettings Display { get; set; } = new();
}

/// <summary>
/// OpenAI API configuration settings.
/// </summary>
public sealed class OpenAISettings
{
    /// <summary>API key for OpenAI. Can also be set via OPENAI_API_KEY environment variable.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Model to use for decisions (e.g., "gpt-4-turbo", "gpt-4o").</summary>
    public string Model { get; set; } = "gpt-4-turbo";

    /// <summary>Maximum tokens for API responses.</summary>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>Temperature for LLM responses (0.0 = deterministic, 1.0 = creative). Default: 0.7</summary>
    public float Temperature { get; set; } = 0.7f;

    /// <summary>Minimum milliseconds between API calls to prevent rate limiting. Default: 1000ms</summary>
    public int MinRequestIntervalMs { get; set; } = 1000;
}

/// <summary>
/// Game-related configuration settings.
/// </summary>
public sealed class GameSettings
{
    /// <summary>URL of the Universal Paperclips game.</summary>
    public string Url { get; set; } = "https://www.decisionproblem.com/paperclips/index2.html";

    /// <summary>Interval in milliseconds between game state captures.</summary>
    public int PollIntervalMs { get; set; } = 500;

    /// <summary>Minimum interval in milliseconds between LLM decisions.</summary>
    public int DecisionIntervalMs { get; set; } = 3000;

    /// <summary>Maximum number of actions per decision.</summary>
    public int MaxActionsPerDecision { get; set; } = 5;
}

/// <summary>
/// Validates configuration values and returns a list of error messages.
/// Returns an empty list if all values are valid.
/// </summary>
public static class ConfigurationValidator
{
    public static List<string> Validate(AppConfiguration config)
    {
        var errors = new List<string>();

        if (config.OpenAI.MaxTokens <= 0)
            errors.Add($"OpenAI.MaxTokens must be positive (got {config.OpenAI.MaxTokens})");

        if (config.OpenAI.Temperature < 0f || config.OpenAI.Temperature > 2f)
            errors.Add($"OpenAI.Temperature must be between 0.0 and 2.0 (got {config.OpenAI.Temperature})");

        if (config.OpenAI.MinRequestIntervalMs < 0)
            errors.Add($"OpenAI.MinRequestIntervalMs cannot be negative (got {config.OpenAI.MinRequestIntervalMs})");

        if (config.Game.PollIntervalMs <= 0)
            errors.Add($"Game.PollIntervalMs must be positive (got {config.Game.PollIntervalMs})");

        if (config.Game.DecisionIntervalMs <= 0)
            errors.Add($"Game.DecisionIntervalMs must be positive (got {config.Game.DecisionIntervalMs})");

        if (config.Game.MaxActionsPerDecision <= 0)
            errors.Add($"Game.MaxActionsPerDecision must be positive (got {config.Game.MaxActionsPerDecision})");

        return errors;
    }
}

/// <summary>
/// Display and UI configuration settings.
/// </summary>
public sealed class DisplaySettings
{
    /// <summary>Whether to show the browser window.</summary>
    public bool ShowBrowser { get; set; } = true;

    /// <summary>Whether to show the console dashboard.</summary>
    public bool ShowConsole { get; set; } = true;

    /// <summary>Whether to show the in-game AI overlay.</summary>
    public bool OverlayEnabled { get; set; } = true;
}
