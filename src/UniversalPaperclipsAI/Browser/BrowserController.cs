using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using UniversalPaperclipsAI.Core;
using UniversalPaperclipsAI.Utilities;

namespace UniversalPaperclipsAI.Browser;

/// <summary>
/// Manages Playwright browser instance for game automation.
/// </summary>
public class BrowserController : IAsyncDisposable
{
    // Configuration constants (Issue #7)
    private const int DefaultSlowMoMs = 50;
    private const int DefaultViewportWidth = 1400;
    private const int DefaultViewportHeight = 900;
    private const int NavigationTimeoutMs = 30000;
    private const int GameInitTimeoutMs = 10000;
    private const int ClickTimeoutMs = 1000;
    private const int QuickClickTimeoutMs = 500;

    private readonly GameSettings _settings;
    private readonly DisplaySettings _displaySettings;
    private readonly ILogger<BrowserController>? _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;

    /// <summary>
    /// Gets the Playwright page instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when browser is not initialized.</exception>
    public IPage Page => _page ?? throw new InvalidOperationException("Browser not initialized. Call InitializeAsync first.");

    /// <summary>
    /// Gets whether the browser has been initialized.
    /// </summary>
    public bool IsInitialized => _page != null;

    /// <summary>
    /// Initializes a new instance of the BrowserController.
    /// </summary>
    public BrowserController(GameSettings settings, DisplaySettings displaySettings, ILogger<BrowserController>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(displaySettings);

        _settings = settings;
        _displaySettings = displaySettings;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the browser and navigates to the game.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Initializing Playwright...");
        _playwright = await Playwright.CreateAsync();

        _logger?.LogInformation("Launching browser...");
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !_displaySettings.ShowBrowser,
            SlowMo = DefaultSlowMoMs,
            Args = new[]
            {
                $"--window-size={DefaultViewportWidth},{DefaultViewportHeight}",
                "--disable-blink-features=AutomationControlled"
            }
        });

        _logger?.LogInformation("Creating new page...");
        _page = await _browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = DefaultViewportWidth, Height = DefaultViewportHeight }
        });

        // Validate URL scheme (Issue #30)
        if (!_settings.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            _logger?.LogWarning("Game URL is not using HTTPS: {Url}", _settings.Url);
        }

        // Navigate to the game
        _logger?.LogInformation("Navigating to {Url}...", _settings.Url);
        await _page.GotoAsync(_settings.Url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = NavigationTimeoutMs
        });

        // Wait for game to initialize
        await _page.WaitForSelectorAsync("#clips", new PageWaitForSelectorOptions
        {
            Timeout = GameInitTimeoutMs
        });

        _logger?.LogInformation("Game loaded successfully!");
    }

    /// <summary>
    /// Clicks an element by selector.
    /// </summary>
    /// <param name="selector">CSS selector for the element.</param>
    /// <returns>True if click succeeded, false otherwise.</returns>
    public virtual async Task<bool> ClickAsync(string selector)
    {
        try
        {
            await Page.ClickAsync(selector, new PageClickOptions
            {
                Timeout = ClickTimeoutMs
            });
            return true;
        }
        catch (TimeoutException ex)
        {
            _logger?.LogDebug(ex, "Click timeout for selector {Selector}", selector);
            return false;
        }
        catch (PlaywrightException ex)
        {
            _logger?.LogDebug(ex, "Click failed for selector {Selector}", selector);
            return false;
        }
    }

    /// <summary>
    /// Clicks an element if it exists and is enabled.
    /// </summary>
    /// <param name="selector">CSS selector for the element.</param>
    /// <returns>True if click succeeded, false otherwise.</returns>
    public virtual async Task<bool> ClickIfEnabledAsync(string selector)
    {
        try
        {
            var element = await Page.QuerySelectorAsync(selector);
            if (element == null)
            {
                _logger?.LogDebug("Element not found: {Selector}", selector);
                return false;
            }

            var isDisabled = await element.GetAttributeAsync("disabled");
            var isVisible = await element.IsVisibleAsync();

            if (isVisible && isDisabled == null)
            {
                await element.ClickAsync(new ElementHandleClickOptions { Timeout = QuickClickTimeoutMs });
                return true;
            }

            _logger?.LogDebug("Element not clickable (visible={IsVisible}, disabled={IsDisabled}): {Selector}",
                isVisible, isDisabled != null, selector);
            return false;
        }
        catch (PlaywrightException ex)
        {
            _logger?.LogDebug(ex, "ClickIfEnabled failed for selector {Selector}", selector);
            return false;
        }
    }

    /// <summary>
    /// Sets a slider value.
    /// </summary>
    /// <param name="selector">CSS selector for the slider.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>True if succeeded, false otherwise.</returns>
    public virtual async Task<bool> SetSliderAsync(string selector, int value)
    {
        try
        {
            var slider = await Page.QuerySelectorAsync(selector);
            if (slider == null)
            {
                _logger?.LogDebug("Slider not found: {Selector}", selector);
                return false;
            }

            await slider.FillAsync(value.ToString());
            // Trigger change event using safe parameter passing
            await Page.EvaluateAsync(@"(selector) => {
                const el = document.querySelector(selector);
                if (el) el.dispatchEvent(new Event('input'));
            }", selector);
            return true;
        }
        catch (PlaywrightException ex)
        {
            _logger?.LogDebug(ex, "SetSlider failed for selector {Selector}", selector);
            return false;
        }
    }

    /// <summary>
    /// Evaluates JavaScript in the page context.
    /// </summary>
    public async Task<T> EvaluateAsync<T>(string script)
    {
        return await Page.EvaluateAsync<T>(script);
    }

    /// <summary>
    /// Injects the AI overlay into the game page.
    /// </summary>
    /// <param name="html">HTML content for the overlay.</param>
    /// <param name="css">CSS styles for the overlay.</param>
    public async Task InjectOverlayAsync(string html, string css)
    {
        // Playwright's parameterized EvaluateAsync safely serializes arguments as JSON,
        // so no manual escaping is needed (manual escaping would cause double-escaping).
        await Page.EvaluateAsync(@"({html, css}) => {
            // Remove existing overlay
            const existing = document.getElementById('ai-overlay');
            if (existing) existing.remove();

            // Add styles
            const style = document.createElement('style');
            style.textContent = css;
            document.head.appendChild(style);

            // Add overlay
            const overlay = document.createElement('div');
            overlay.id = 'ai-overlay';
            overlay.innerHTML = html;
            document.body.appendChild(overlay);
        }", new { html, css });
    }

    /// <summary>
    /// Updates content in the browser overlay.
    /// Note: Caller is responsible for escaping user-generated content.
    /// </summary>
    /// <param name="elementId">ID of the element to update.</param>
    /// <param name="content">Pre-sanitized HTML content.</param>
    public async Task UpdateOverlayContentAsync(string elementId, string content)
    {
        // Only sanitize the element ID to prevent injection
        var safeElementId = StringSanitizer.SanitizeElementId(elementId);

        await Page.EvaluateAsync(@"({elementId, content}) => {
            const el = document.getElementById(elementId);
            if (el) el.innerHTML = content;
        }", new { elementId = safeElementId, content });
    }

    /// <summary>
    /// Updates text content in the browser overlay (auto-escaped).
    /// </summary>
    /// <param name="elementId">ID of the element to update.</param>
    /// <param name="text">Text content (will be HTML-escaped).</param>
    public async Task UpdateOverlayTextAsync(string elementId, string text)
    {
        var safeElementId = StringSanitizer.SanitizeElementId(elementId);
        // textContent is already text-safe (treats input as plain text, not HTML),
        // so HTML-escaping is unnecessary and would cause entities to display literally.
        await Page.EvaluateAsync(@"({elementId, text}) => {
            const el = document.getElementById(elementId);
            if (el) el.textContent = text;
        }", new { elementId = safeElementId, text });
    }

    public async ValueTask DisposeAsync()
    {
        if (_page != null)
        {
            await _page.CloseAsync();
            _page = null;
        }

        if (_browser != null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;
    }
}
