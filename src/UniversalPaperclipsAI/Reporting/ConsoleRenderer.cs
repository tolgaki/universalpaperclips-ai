using Spectre.Console;
using Spectre.Console.Rendering;
using UniversalPaperclipsAI.Actions;
using UniversalPaperclipsAI.AI;
using UniversalPaperclipsAI.GameState;
using UniversalPaperclipsAI.Utilities;

namespace UniversalPaperclipsAI.Reporting;

/// <summary>
/// Renders game state and AI decisions to the console using Spectre.Console.
/// </summary>
public sealed class ConsoleRenderer
{
    private readonly object _lock = new();
    private readonly DateTime _startTime = DateTime.UtcNow;
    private bool _isFirstRender = true;

    // ANSI escape codes for cursor control (reduces flicker vs Console.Clear)
    private const string CursorHome = "\u001b[H";        // Move cursor to home position (0,0)
    private const string ClearToEndOfScreen = "\u001b[J"; // Clear from cursor to end of screen

    public void RenderDashboard(
        GameStateSnapshot state,
        DecisionLogEntry? lastDecision,
        IEnumerable<ActionLogEntry> recentActions)
    {
        lock (_lock)
        {
            // Only clear on first render; subsequent renders use cursor repositioning
            if (_isFirstRender)
            {
                Console.Clear();
                _isFirstRender = false;
            }
            else
            {
                // Move cursor to top-left without clearing (reduces flicker)
                Console.Write(CursorHome);
            }

            AnsiConsole.Write(CreateDashboard(state, lastDecision, recentActions));

            // Clear any leftover content from previous longer renders
            Console.Write(ClearToEndOfScreen);
        }
    }

    private Panel CreateDashboard(
        GameStateSnapshot state,
        DecisionLogEntry? lastDecision,
        IEnumerable<ActionLogEntry> recentActions)
    {
        var elapsed = DateTime.UtcNow - _startTime;

        // Build the layout
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        // Header
        var header = new Rule($"[bold cyan]Universal Paperclips AI Controller[/]")
        {
            Justification = Justify.Center
        };

        // Status bar
        var statusTable = new Table()
            .Border(TableBorder.None)
            .AddColumn("")
            .AddColumn("")
            .AddColumn("")
            .AddColumn("");

        statusTable.AddRow(
            $"[yellow]Phase:[/] {state.Phase}",
            $"[yellow]Runtime:[/] {elapsed:hh\\:mm\\:ss}",
            $"[yellow]State:[/] {state.Timestamp:HH:mm:ss}",
            $"[yellow]Decisions:[/] {(lastDecision != null ? "Active" : "Waiting")}"
        );

        // Resources panel
        var resourcesPanel = CreateResourcesPanel(state);

        // Manufacturing panel
        var manufacturingPanel = CreateManufacturingPanel(state);

        // AI Decision panel
        var aiPanel = CreateAIDecisionPanel(lastDecision);

        // Action History panel
        var actionsPanel = CreateActionsPanel(recentActions);

        // Combine into layout
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Header").Size(3),
                new Layout("Status").Size(3),
                new Layout("Main").SplitColumns(
                    new Layout("Left").SplitRows(
                        new Layout("Resources"),
                        new Layout("Manufacturing")
                    ),
                    new Layout("Right").SplitRows(
                        new Layout("AI"),
                        new Layout("Actions")
                    )
                )
            );

        layout["Header"].Update(header);
        layout["Status"].Update(statusTable);
        layout["Resources"].Update(resourcesPanel);
        layout["Manufacturing"].Update(manufacturingPanel);
        layout["AI"].Update(aiPanel);
        layout["Actions"].Update(actionsPanel);

        return new Panel(layout)
            .Border(BoxBorder.Double)
            .Header("[bold white] AI Paperclip Controller [/]");
    }

    private Panel CreateResourcesPanel(GameStateSnapshot state)
    {
        var table = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("Resource")
            .AddColumn("Value");

        table.AddRow("[cyan]Paperclips[/]", $"[bold]{state.Resources.Paperclips:N0}[/]");
        table.AddRow("[yellow]Unsold[/]", $"{state.Resources.UnsoldClips:N0}");
        table.AddRow("[green]Funds[/]", $"[bold]${state.Resources.Funds:N2}[/]");
        table.AddRow("[blue]Wire[/]", WireStatus(state.Resources.Wire));
        table.AddRow("[magenta]Trust[/]", $"{state.Resources.Trust}");
        table.AddRow("[white]Operations[/]", $"{state.Computing.Operations:N0}/{state.Computing.MaxOperations:N0}");

        if (state.Computing.Creativity > 0)
            table.AddRow("[yellow]Creativity[/]", $"{state.Computing.Creativity:N0}");

        if (state.Resources.Yomi > 0)
            table.AddRow("[cyan]Yomi[/]", $"{state.Resources.Yomi:N0}");

        return new Panel(table)
            .Header("[bold green] Resources [/]")
            .Border(BoxBorder.Rounded);
    }

    private string WireStatus(double wire)
    {
        var color = wire switch
        {
            < 100 => "red",
            < 500 => "yellow",
            _ => "green"
        };
        return $"[{color}]{wire:N0}\"[/]";
    }

    private Panel CreateManufacturingPanel(GameStateSnapshot state)
    {
        var table = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("Metric")
            .AddColumn("Value");

        table.AddRow("[cyan]Rate[/]", $"[bold]{state.Manufacturing.ClipsPerSecond:N1}/sec[/]");
        table.AddRow("[yellow]AutoClippers[/]", $"{state.Manufacturing.AutoClippers}");

        if (state.Manufacturing.MegaClippersUnlocked)
            table.AddRow("[magenta]MegaClippers[/]", $"{state.Manufacturing.MegaClippers}");

        table.AddRow("[green]Price[/]", $"${state.Business.Price:N2}");
        table.AddRow("[blue]Demand[/]", DemandBar(state.Business.Demand));
        table.AddRow("[white]Marketing[/]", $"Lvl {state.Business.MarketingLevel}");

        // Investments if unlocked
        if (state.Investments.IsUnlocked)
        {
            table.AddRow("", ""); // Spacer
            table.AddRow("[gold1]Portfolio[/]", $"${state.Investments.PortfolioValue:N2}");
        }

        // Space if unlocked
        if (state.Space.IsUnlocked)
        {
            table.AddRow("", ""); // Spacer
            table.AddRow("[purple]Probes[/]", $"{state.Space.ProbeCount:N0}");
            table.AddRow("[blue]Explored[/]", $"{state.Space.ExplorationPercent:P2}");
        }

        return new Panel(table)
            .Header("[bold yellow] Manufacturing [/]")
            .Border(BoxBorder.Rounded);
    }

    private string DemandBar(double demand)
    {
        var filled = (int)(demand * 10);
        var bar = new string('█', Math.Min(filled, 10)) + new string('░', Math.Max(0, 10 - filled));
        var color = demand switch
        {
            < 0.3 => "red",
            < 0.6 => "yellow",
            _ => "green"
        };
        return $"[{color}]{bar}[/] {demand:P0}";
    }

    private Panel CreateAIDecisionPanel(DecisionLogEntry? lastDecision)
    {
        if (lastDecision == null)
        {
            return new Panel(new Markup("[grey]Waiting for first decision...[/]"))
                .Header("[bold magenta] AI Decision [/]")
                .Border(BoxBorder.Rounded);
        }

        var content = new List<IRenderable>();

        content.Add(new Markup($"[bold cyan]Priority:[/] {Markup.Escape(lastDecision.Priority)}"));
        content.Add(new Markup(""));
        content.Add(new Markup($"[bold yellow]Reasoning:[/]"));
        content.Add(new Markup($"[white]{Markup.Escape(StringSanitizer.Truncate(lastDecision.Reasoning, 200))}[/]"));
        content.Add(new Markup(""));
        content.Add(new Markup($"[bold green]Actions ({lastDecision.Actions.Count}):[/]"));

        foreach (var action in lastDecision.Actions.Take(5))
        {
            content.Add(new Markup($"  [cyan]→[/] {Markup.Escape(action.Type)}"));
        }

        return new Panel(new Rows(content))
            .Header("[bold magenta] AI Decision [/]")
            .Border(BoxBorder.Rounded);
    }

    private Panel CreateActionsPanel(IEnumerable<ActionLogEntry> recentActions)
    {
        var actions = recentActions.TakeLast(8).Reverse().ToList();

        if (!actions.Any())
        {
            return new Panel(new Markup("[grey]No actions yet...[/]"))
                .Header("[bold blue] Action History [/]")
                .Border(BoxBorder.Rounded);
        }

        var table = new Table()
            .Border(TableBorder.None)
            .AddColumn("Time")
            .AddColumn("Action")
            .AddColumn("Result");

        foreach (var action in actions)
        {
            var statusColor = action.Success ? "green" : "red";
            var status = action.Success ? "✓" : "✗";

            table.AddRow(
                $"[grey]{action.Timestamp:HH:mm:ss}[/]",
                $"[cyan]{action.ActionName}[/]",
                $"[{statusColor}]{status}[/]"
            );
        }

        return new Panel(table)
            .Header("[bold blue] Action History [/]")
            .Border(BoxBorder.Rounded);
    }

    public void LogMessage(string message, string color = "white")
    {
        lock (_lock)
        {
            AnsiConsole.MarkupLine($"[{color}]{Markup.Escape(message)}[/]");
        }
    }

    public void LogDecision(DecisionLogEntry decision)
    {
        lock (_lock)
        {
            AnsiConsole.MarkupLine($"[cyan]═══ AI Decision ═══[/]");
            AnsiConsole.MarkupLine($"[yellow]Priority:[/] {Markup.Escape(decision.Priority)}");
            AnsiConsole.MarkupLine($"[white]Reasoning:[/] {Markup.Escape(decision.Reasoning)}");
            AnsiConsole.MarkupLine($"[green]Actions:[/] {Markup.Escape(string.Join(", ", decision.Actions.Select(a => a.Type)))}");
            AnsiConsole.MarkupLine($"[cyan]══════════════════[/]");
        }
    }
}
