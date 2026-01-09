namespace UniversalPaperclipsAI.Actions;

/// <summary>
/// Represents the result of executing a game action.
/// </summary>
/// <param name="Success">Whether the action succeeded.</param>
/// <param name="Message">Description of the result or error.</param>
public record ActionResult(bool Success, string Message);
