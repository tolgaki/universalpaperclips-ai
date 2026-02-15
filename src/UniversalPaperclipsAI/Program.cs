using Microsoft.Extensions.Configuration;
using Spectre.Console;
using UniversalPaperclipsAI.AI;
using UniversalPaperclipsAI.Browser;
using UniversalPaperclipsAI.Core;

namespace UniversalPaperclipsAI;

/// <summary>
/// Entry point for the Universal Paperclips AI Controller.
/// </summary>
internal static class Program
{
    private const int StartupDelayMs = 2000;

    /// <summary>
    /// Main entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code (0 for success, non-zero for error).</returns>
    public static async Task<int> Main(string[] args)
    {
        AnsiConsole.Write(
            new FigletText("Paperclip AI")
                .Centered()
                .Color(Color.Cyan1));

        AnsiConsole.MarkupLine("[grey]Universal Paperclips AI Controller[/]");
        AnsiConsole.MarkupLine("[grey]Press Ctrl+C to stop[/]");
        AnsiConsole.WriteLine();

        try
        {
            // Load configuration
            var config = LoadConfiguration();

            // Validate configuration
            var validationResult = ValidateConfiguration(config);
            if (validationResult != 0)
            {
                return validationResult;
            }

            PrintConfiguration(config);

            // Set up cancellation
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                AnsiConsole.MarkupLine("\n[yellow]Shutting down...[/]");
                cts.Cancel();
            };

            // Initialize and run
            return await RunAsync(config, cts.Token);
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Operation cancelled by user.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Fatal error: {Markup.Escape(ex.Message)}[/]");
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static AppConfiguration LoadConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true) // Local overrides (gitignored)
            .AddEnvironmentVariables()
            .Build();

        var config = new AppConfiguration();
        configuration.Bind(config);

        // Override API key from environment if not set in config
        if (string.IsNullOrEmpty(config.OpenAI.ApiKey))
        {
            config.OpenAI.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
        }

        return config;
    }

    private static int ValidateConfiguration(AppConfiguration config)
    {
        if (string.IsNullOrEmpty(config.OpenAI.ApiKey))
        {
            AnsiConsole.MarkupLine("[red]Error: OpenAI API key not configured.[/]");
            AnsiConsole.MarkupLine("[yellow]Set the OPENAI_API_KEY environment variable or configure in appsettings.json[/]");
            return 1;
        }

        if (string.IsNullOrEmpty(config.Game.Url))
        {
            AnsiConsole.MarkupLine("[red]Error: Game URL not configured.[/]");
            return 1;
        }

        if (config.Game.PollIntervalMs < 100)
        {
            AnsiConsole.MarkupLine("[yellow]Warning: Poll interval is very low, this may cause high CPU usage.[/]");
        }

        // Validate configuration bounds
        var configErrors = ConfigurationValidator.Validate(config);
        foreach (var error in configErrors)
        {
            AnsiConsole.MarkupLine($"[red]Config error: {Markup.Escape(error)}[/]");
        }
        if (configErrors.Count > 0)
        {
            return 1;
        }

        return 0;
    }

    private static void PrintConfiguration(AppConfiguration config)
    {
        AnsiConsole.MarkupLine("[green]✓[/] Configuration loaded");
        AnsiConsole.MarkupLine($"[green]✓[/] OpenAI Model: {Markup.Escape(config.OpenAI.Model)}");
        AnsiConsole.MarkupLine($"[green]✓[/] Game URL: {Markup.Escape(config.Game.Url)}");
        AnsiConsole.MarkupLine($"[green]✓[/] Decision Interval: {config.Game.DecisionIntervalMs}ms");
        AnsiConsole.WriteLine();
    }

    private static async Task<int> RunAsync(AppConfiguration config, CancellationToken cancellationToken)
    {
        // Initialize browser
        await using var browser = new BrowserController(config.Game, config.Display);

        await AnsiConsole.Status()
            .StartAsync("Initializing browser...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                await browser.InitializeAsync(cancellationToken);
            });

        AnsiConsole.MarkupLine("[green]✓[/] Browser initialized and game loaded");
        AnsiConsole.WriteLine();

        // Create decision engine
        using var decisionEngine = new DecisionEngine(config.OpenAI);
        AnsiConsole.MarkupLine("[green]✓[/] AI Decision Engine ready");

        // Create and run game loop
        using var gameLoop = new GameLoop(browser, decisionEngine, config.Game, config.Display);

        AnsiConsole.MarkupLine("[cyan]Starting AI control loop...[/]");
        AnsiConsole.WriteLine();

        // Give user a moment to see the startup messages
        await Task.Delay(StartupDelayMs, cancellationToken);

        await gameLoop.RunAsync(cancellationToken);

        AnsiConsole.MarkupLine($"[green]Game loop completed. Total decisions: {gameLoop.TotalDecisions}[/]");
        return 0;
    }
}
