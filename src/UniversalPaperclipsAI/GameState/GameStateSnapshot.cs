using UniversalPaperclipsAI.GameState.Models;

namespace UniversalPaperclipsAI.GameState;

public class GameStateSnapshot
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public GamePhase Phase { get; set; } = GamePhase.Early;
    public Resources Resources { get; set; } = new();
    public Manufacturing Manufacturing { get; set; } = new();
    public Business Business { get; set; } = new();
    public Computing Computing { get; set; } = new();
    public ProjectList Projects { get; set; } = new();
    public Investments Investments { get; set; } = new();
    public SpacePhase Space { get; set; } = new();
    public List<string> AvailableActions { get; set; } = new();
    public List<string> RecentMessages { get; set; } = new();

    public string ToSummary() => $"""
        === Game State ({Timestamp:HH:mm:ss}) ===
        Phase: {Phase}
        {Resources}
        {Manufacturing}
        {Business}
        {Computing}
        {Projects}
        {Investments}
        {Space}
        Available Actions: {AvailableActions.Count}
        """;
}

public enum GamePhase
{
    Early,      // Manual clicking, getting first autoclippers
    Middle,     // Full automation, investments, mega clippers
    Late,       // Space phase, probes, universe consumption
    Complete    // Game won
}
