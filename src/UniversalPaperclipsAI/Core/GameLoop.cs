using Microsoft.Extensions.Logging;
using UniversalPaperclipsAI.Actions;
using UniversalPaperclipsAI.AI;
using UniversalPaperclipsAI.Browser;
using UniversalPaperclipsAI.GameState;
using UniversalPaperclipsAI.Reporting;

namespace UniversalPaperclipsAI.Core;

/// <summary>
/// Main game control loop that orchestrates state observation, decision making, and action execution.
/// </summary>
public sealed class GameLoop : IDisposable
{
    private readonly BrowserController _browser;
    private readonly GameStateObserver _observer;
    private readonly EventDetector _eventDetector;
    private readonly DecisionEngine _decisionEngine;
    private readonly ActionExecutor _actionExecutor;
    private readonly ConsoleRenderer _consoleRenderer;
    private readonly BrowserOverlay _browserOverlay;
    private readonly GameSettings _gameSettings;
    private readonly DisplaySettings _displaySettings;
    private readonly ILogger<GameLoop>? _logger;

    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private int _totalDecisions;
    private bool _disposed;

    /// <summary>
    /// Gets the total number of decisions made.
    /// </summary>
    public int TotalDecisions => _totalDecisions;

    /// <summary>
    /// Gets whether the game loop is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Initializes a new instance of the GameLoop.
    /// </summary>
    public GameLoop(
        BrowserController browser,
        DecisionEngine decisionEngine,
        GameSettings gameSettings,
        DisplaySettings displaySettings,
        ILogger<GameLoop>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(browser);
        ArgumentNullException.ThrowIfNull(decisionEngine);
        ArgumentNullException.ThrowIfNull(gameSettings);
        ArgumentNullException.ThrowIfNull(displaySettings);

        _browser = browser;
        _decisionEngine = decisionEngine;
        _gameSettings = gameSettings;
        _displaySettings = displaySettings;
        _logger = logger;

        _observer = new GameStateObserver(browser.Page, logger: null);
        _eventDetector = new EventDetector(gameSettings.DecisionIntervalMs);
        _actionExecutor = new ActionExecutor(browser, gameSettings.MaxActionsPerDecision);
        _consoleRenderer = new ConsoleRenderer();
        _browserOverlay = new BrowserOverlay(browser);
    }

    /// <summary>
    /// Runs the main game loop.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the loop.</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isRunning = true;

        _logger?.LogInformation("Starting game loop...");

        // Initialize browser overlay
        if (_displaySettings.OverlayEnabled)
        {
            await _browserOverlay.InitializeAsync();
        }

        try
        {
            while (_isRunning && !_cts.Token.IsCancellationRequested)
            {
                await TickAsync(_cts.Token);
                await Task.Delay(_gameSettings.PollIntervalMs, _cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("Game loop cancelled.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Game loop error");
            throw;
        }
        finally
        {
            _isRunning = false;
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Capture current game state
            var state = await _observer.CaptureStateAsync();

            // Check if we should trigger a decision
            if (_eventDetector.ShouldTriggerDecision(state))
            {
                var reasons = _eventDetector.GetTriggerReasons(state);

                if (_displaySettings.ShowConsole)
                {
                    _consoleRenderer.LogMessage(
                        $"Decision triggered: {string.Join(", ", reasons)}",
                        "yellow");
                }

                // Get decision from LLM (with cancellation support)
                var decision = await _decisionEngine.MakeDecisionAsync(
                    state,
                    _actionExecutor.GetRecentHistory(),
                    cancellationToken);

                _totalDecisions++;

                // Log the decision
                if (_displaySettings.ShowConsole && _decisionEngine.LastDecision != null)
                {
                    _consoleRenderer.LogDecision(_decisionEngine.LastDecision);
                }

                // Execute the actions with validation (with cancellation support)
                await _actionExecutor.ExecuteBatchAsync(decision.Actions, state.AvailableActions, cancellationToken);

                // Update overlay
                if (_displaySettings.OverlayEnabled)
                {
                    await _browserOverlay.UpdateAsync(
                        state,
                        _decisionEngine.LastDecision,
                        _totalDecisions);
                }
            }

            // Update console dashboard
            if (_displaySettings.ShowConsole)
            {
                _consoleRenderer.RenderDashboard(
                    state,
                    _decisionEngine.LastDecision,
                    _actionExecutor.GetRecentHistory());
            }

            // Update overlay periodically even without new decisions
            if (_displaySettings.OverlayEnabled && _totalDecisions > 0)
            {
                await _browserOverlay.UpdateAsync(
                    state,
                    _decisionEngine.LastDecision,
                    _totalDecisions);
            }
        }
        catch (OperationCanceledException)
        {
            throw; // Propagate cancellation
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Tick error");
            if (_displaySettings.ShowConsole)
            {
                _consoleRenderer.LogMessage($"Tick error: {ex.Message}", "red");
            }
        }
    }

    /// <summary>
    /// Stops the game loop.
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();
    }

    /// <summary>
    /// Disposes resources used by the game loop.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _disposed = true;
    }
}
