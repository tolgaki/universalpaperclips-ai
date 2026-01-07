namespace UniversalPaperclipsAI.Actions;

public interface IGameAction
{
    string Name { get; }
    string Description { get; }
    Task ExecuteAsync(ActionContext context);
}

public class ActionContext
{
    public required Browser.BrowserController Browser { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = new();
}

public record ActionResult(bool Success, string Message);
