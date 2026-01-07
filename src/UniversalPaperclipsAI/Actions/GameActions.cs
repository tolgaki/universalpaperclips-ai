namespace UniversalPaperclipsAI.Actions;

public static class GameActions
{
    public static readonly Dictionary<string, GameActionDefinition> All = new()
    {
        // Manufacturing
        ["MakePaperclip"] = new("MakePaperclip", "#btnMakePaperclip", "Click to make one paperclip manually"),
        ["MakePaperclip10"] = new("MakePaperclip10", "#btnMakePaperclip", "Make 10 paperclips (10 clicks)", ClickCount: 10),
        ["BuyWire"] = new("BuyWire", "#btnBuyWire", "Purchase more wire"),
        ["BuyAutoClipper"] = new("BuyAutoClipper", "#btnMakeClipper", "Buy an AutoClipper"),
        ["BuyMegaClipper"] = new("BuyMegaClipper", "#btnMakeMegaClipper", "Buy a MegaClipper"),

        // Business
        ["RaisePrice"] = new("RaisePrice", "#btnRaisePrice", "Increase paperclip price"),
        ["LowerPrice"] = new("LowerPrice", "#btnLowerPrice", "Decrease paperclip price"),
        ["BuyMarketing"] = new("BuyMarketing", "#btnExpandMarketing", "Increase marketing level"),

        // Computing
        ["BuyProcessor"] = new("BuyProcessor", "#btnAddProc", "Add a processor"),
        ["BuyMemory"] = new("BuyMemory", "#btnAddMem", "Add memory"),

        // Trust allocation (uses quantum computing)
        ["QuantumCompute"] = new("QuantumCompute", "#btnQcompute", "Run quantum computation"),

        // Investment
        ["Deposit"] = new("Deposit", "#btnInvest", "Deposit funds into investment engine"),
        ["Withdraw"] = new("Withdraw", "#btnWithdraw", "Withdraw from investment engine"),
        ["SetLowRisk"] = new("SetLowRisk", "#risk", "Set investment to low risk", IsSlider: true, SliderValue: 2),
        ["SetMediumRisk"] = new("SetMediumRisk", "#risk", "Set investment to medium risk", IsSlider: true, SliderValue: 5),
        ["SetHighRisk"] = new("SetHighRisk", "#risk", "Set investment to high risk", IsSlider: true, SliderValue: 8),

        // Space Phase
        ["LaunchProbe"] = new("LaunchProbe", "#btnMakeProbe", "Launch a probe"),
        ["MakeFactory"] = new("MakeFactory", "#btnMakeFactory", "Build a factory"),
        ["MakeHarvester"] = new("MakeHarvester", "#btnMakeHarvester", "Build a harvester drone"),
        ["MakeWireDrone"] = new("MakeWireDrone", "#btnMakeWireDrone", "Build a wire drone"),

        // Combat/Honor
        ["Fight"] = new("Fight", "#btnFight", "Engage in combat"),

        // Swarm Computing
        ["Entertain"] = new("Entertain", "#btnEntertain", "Entertain the swarm"),
        ["Synchronize"] = new("Synchronize", "#btnSynchronize", "Synchronize the swarm"),

        // Strategic Modeling
        ["RunTournament"] = new("RunTournament", "#btnRunTournament", "Run a tournament"),
        ["NewTournament"] = new("NewTournament", "#btnNewTourney", "Start new tournament"),

        // Toggle buttons
        ["ToggleAutoWire"] = new("ToggleAutoWire", "#btnToggleWire", "Toggle auto wire buyer"),
        ["ToggleAutoTourney"] = new("ToggleAutoTourney", "#btnToggleAutoTourney", "Toggle auto tournaments"),
    };

    public static IEnumerable<string> GetActionNames() => All.Keys;

    public static GameActionDefinition? Get(string name) =>
        All.TryGetValue(name, out var action) ? action : null;
}

public record GameActionDefinition(
    string Name,
    string Selector,
    string Description,
    int ClickCount = 1,
    bool IsSlider = false,
    int SliderValue = 0
);
