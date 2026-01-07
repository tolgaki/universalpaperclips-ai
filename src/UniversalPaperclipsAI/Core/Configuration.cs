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
