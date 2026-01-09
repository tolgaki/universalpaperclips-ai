using Microsoft.Extensions.Logging;
using UniversalPaperclipsAI.AI;
using UniversalPaperclipsAI.Browser;
using UniversalPaperclipsAI.GameState;
using UniversalPaperclipsAI.Utilities;

namespace UniversalPaperclipsAI.Reporting;

/// <summary>
/// Renders an AI status overlay in the browser window.
/// </summary>
public sealed class BrowserOverlay
{
    private readonly BrowserController _browser;
    private readonly ILogger<BrowserOverlay>? _logger;
    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the BrowserOverlay.
    /// </summary>
    public BrowserOverlay(BrowserController browser, ILogger<BrowserOverlay>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(browser);
        _browser = browser;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        const string css = @"
            #ai-overlay {
                position: fixed;
                top: 10px;
                right: 10px;
                width: 320px;
                background: rgba(0, 0, 0, 0.85);
                border: 2px solid #00ffff;
                border-radius: 8px;
                padding: 12px;
                font-family: 'Courier New', monospace;
                font-size: 12px;
                color: #fff;
                z-index: 10000;
                box-shadow: 0 0 20px rgba(0, 255, 255, 0.3);
            }
            #ai-overlay h3 {
                margin: 0 0 10px 0;
                color: #00ffff;
                font-size: 14px;
                border-bottom: 1px solid #00ffff;
                padding-bottom: 5px;
            }
            #ai-overlay .status-row {
                display: flex;
                justify-content: space-between;
                margin: 4px 0;
            }
            #ai-overlay .label {
                color: #888;
            }
            #ai-overlay .value {
                color: #0f0;
                font-weight: bold;
            }
            #ai-overlay .priority {
                color: #ff0;
                margin: 8px 0;
                padding: 6px;
                background: rgba(255, 255, 0, 0.1);
                border-radius: 4px;
            }
            #ai-overlay .reasoning {
                color: #aaa;
                font-size: 11px;
                margin: 8px 0;
                max-height: 80px;
                overflow-y: auto;
            }
            #ai-overlay .actions {
                margin-top: 8px;
                padding-top: 8px;
                border-top: 1px solid #333;
            }
            #ai-overlay .action-item {
                color: #0ff;
                padding: 2px 0;
            }
            #ai-overlay .action-item::before {
                content: '→ ';
                color: #0f0;
            }
        ";

        const string html = @"
            <h3>🤖 AI Controller</h3>
            <div id='ai-status'>
                <div class='status-row'>
                    <span class='label'>Status:</span>
                    <span class='value' id='ai-running-status'>Initializing...</span>
                </div>
                <div class='status-row'>
                    <span class='label'>Decisions:</span>
                    <span class='value' id='ai-decision-count'>0</span>
                </div>
            </div>
            <div class='priority' id='ai-priority'>Waiting for first decision...</div>
            <div class='reasoning' id='ai-reasoning'></div>
            <div class='actions' id='ai-actions'>
                <strong>Recent Actions:</strong>
                <div id='ai-action-list'></div>
            </div>
        ";

        await _browser.InjectOverlayAsync(html, css);
        _isInitialized = true;
    }

    public async Task UpdateAsync(GameStateSnapshot state, DecisionLogEntry? lastDecision, int totalDecisions)
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        try
        {
            // Update status
            await _browser.UpdateOverlayContentAsync("ai-running-status",
                $"<span style='color: #0f0'>● Running</span> ({state.Phase})");

            await _browser.UpdateOverlayContentAsync("ai-decision-count", totalDecisions.ToString());

            if (lastDecision != null)
            {
                // Update priority
                await _browser.UpdateOverlayContentAsync("ai-priority",
                    $"🎯 {StringSanitizer.EscapeHtml(lastDecision.Priority)}");

                // Update reasoning
                var reasoning = StringSanitizer.Truncate(lastDecision.Reasoning, 150);
                await _browser.UpdateOverlayContentAsync("ai-reasoning",
                    $"💭 {StringSanitizer.EscapeHtml(reasoning)}");

                // Update actions
                var actionsHtml = string.Join("",
                    lastDecision.Actions.Take(5).Select(a =>
                        $"<div class='action-item'>{StringSanitizer.EscapeHtml(a.Type)}</div>"));
                await _browser.UpdateOverlayContentAsync("ai-action-list", actionsHtml);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Error updating overlay");
        }
    }
}
