using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using UniversalPaperclipsAI.GameState.Models;

namespace UniversalPaperclipsAI.GameState;

/// <summary>
/// Extracts game state from the browser DOM.
/// </summary>
/// <remarks>
/// <para>
/// This class does not own the <see cref="IPage"/> instance and does not implement
/// <see cref="IDisposable"/>. The caller (typically <see cref="Browser.BrowserController"/>)
/// is responsible for managing the page lifecycle.
/// </para>
/// <para>
/// The observer should not be used after the page has been closed or disposed.
/// </para>
/// </remarks>
public sealed class GameStateObserver
{
    private readonly IPage _page;
    private readonly ILogger<GameStateObserver>? _logger;

    /// <summary>
    /// Initializes a new instance of the GameStateObserver.
    /// </summary>
    /// <param name="page">
    /// Playwright page instance. The caller retains ownership and is responsible
    /// for the page lifecycle. This observer should not be used after the page is closed.
    /// </param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public GameStateObserver(IPage page, ILogger<GameStateObserver>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        _page = page;
        _logger = logger;
    }

    public async Task<GameStateSnapshot> CaptureStateAsync()
    {
        var state = new GameStateSnapshot
        {
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // Execute JavaScript to extract all game state at once
            var gameData = await _page.EvaluateAsync<Dictionary<string, object?>>(@"() => {
                const getNum = (id) => {
                    const el = document.getElementById(id);
                    if (!el) return 0;
                    const text = el.textContent || el.innerText || '0';
                    return parseFloat(text.replace(/,/g, '').replace(/[^0-9.-]/g, '')) || 0;
                };

                const getText = (id) => {
                    const el = document.getElementById(id);
                    return el ? (el.textContent || el.innerText || '') : '';
                };

                const isVisible = (id) => {
                    const el = document.getElementById(id);
                    if (!el) return false;
                    const style = window.getComputedStyle(el);
                    return style.display !== 'none' && style.visibility !== 'hidden';
                };

                const isEnabled = (id) => {
                    const el = document.getElementById(id);
                    return el && !el.disabled;
                };

                const getButtonState = (id) => {
                    const el = document.getElementById(id);
                    if (!el) return { visible: false, enabled: false };
                    const style = window.getComputedStyle(el);
                    return {
                        visible: style.display !== 'none' && style.visibility !== 'hidden',
                        enabled: !el.disabled
                    };
                };

                // Get available projects
                const projects = [];
                const projectButtons = document.querySelectorAll('[id^=""project""]');
                projectButtons.forEach(btn => {
                    if (btn.tagName === 'BUTTON' && btn.style.display !== 'none') {
                        const parent = btn.parentElement;
                        const title = parent?.querySelector('.title')?.textContent || btn.textContent || '';
                        const cost = parent?.querySelector('.cost')?.textContent || '';
                        projects.push({
                            id: btn.id,
                            title: title.trim(),
                            cost: cost.trim(),
                            available: !btn.disabled
                        });
                    }
                });

                return {
                    // Resources
                    clips: getNum('clips'),
                    unsoldClips: getNum('unsoldClips'),
                    funds: getNum('funds'),
                    wire: getNum('wire'),
                    trust: getNum('trust'),

                    // Manufacturing
                    clipRate: getNum('clipmakerRate') || getNum('clipRate'),
                    autoClippers: getNum('clipmakerLevel'),
                    megaClippers: getNum('clipmakerLevel2'),
                    autoClipperCost: getNum('clipperCost'),
                    megaClipperCost: getNum('megaClipperCost'),
                    wireCost: getNum('wireCost'),

                    // Business
                    price: getNum('margin'),
                    demand: getNum('demand'),
                    marketingLevel: getNum('marketingLvl'),
                    marketingCost: getNum('adCost'),

                    // Computing
                    processors: getNum('processors'),
                    memory: getNum('memory'),
                    operations: getNum('operations'),
                    maxOperations: getNum('maxOps') || 1000,
                    creativity: getNum('creativity'),
                    processorCost: getNum('procCost'),
                    memoryCost: getNum('memoryCost'),

                    // Investments
                    investmentsVisible: isVisible('investmentEngine'),
                    portfolioValue: getNum('portTotal'),
                    stocksValue: getNum('stocks'),
                    bondsValue: getNum('bonds'),

                    // Space Phase
                    spaceVisible: isVisible('probeDesignDiv') || getNum('probes') > 0,
                    probes: getNum('probes'),
                    matter: getNum('availableMatter'),
                    acquiredMatter: getNum('acquiredMatter'),
                    harvesterDrones: getNum('harvesterDrones') || getNum('drones'),
                    wireDrones: getNum('wireDrones'),
                    exploration: getNum('spaceExploration'),
                    honor: getNum('honor'),

                    // Button states
                    btnMakePaperclip: getButtonState('btnMakePaperclip'),
                    btnBuyWire: getButtonState('btnBuyWire'),
                    btnRaisePrice: getButtonState('btnRaisePrice'),
                    btnLowerPrice: getButtonState('btnLowerPrice'),
                    btnExpandMarketing: getButtonState('btnExpandMarketing'),
                    btnMakeClipper: getButtonState('btnMakeClipper'),
                    btnMakeMegaClipper: getButtonState('btnMakeMegaClipper'),
                    btnAddProc: getButtonState('btnAddProc'),
                    btnAddMem: getButtonState('btnAddMem'),
                    btnDeposit: getButtonState('btnInvest'),
                    btnWithdraw: getButtonState('btnWithdraw'),
                    btnMakeProbe: getButtonState('btnMakeProbe'),

                    // Projects
                    projects: projects,

                    // Yomi for strategic modeling
                    yomi: getNum('yomi'),

                    // Check phase indicators
                    businessDivVisible: isVisible('businessDiv'),
                    probeDivVisible: isVisible('probeDesignDiv')
                };
            }");

            // Map to state objects
            state.Resources = new Resources
            {
                Paperclips = GetLong(gameData, "clips"),
                UnsoldClips = GetLong(gameData, "unsoldClips"),
                Funds = GetDouble(gameData, "funds"),
                Wire = GetDouble(gameData, "wire"),
                Trust = GetInt(gameData, "trust"),
                Memory = GetInt(gameData, "memory"),
                Processors = GetInt(gameData, "processors"),
                Operations = GetDouble(gameData, "operations"),
                Creativity = GetDouble(gameData, "creativity"),
                Yomi = GetLong(gameData, "yomi"),
                Honor = GetLong(gameData, "honor")
            };

            state.Manufacturing = new Manufacturing
            {
                ClipsPerSecond = GetDouble(gameData, "clipRate"),
                AutoClippers = GetInt(gameData, "autoClippers"),
                MegaClippers = GetInt(gameData, "megaClippers"),
                AutoClipperCost = GetDouble(gameData, "autoClipperCost"),
                MegaClipperCost = GetDouble(gameData, "megaClipperCost"),
                WireCost = GetDouble(gameData, "wireCost"),
                CanMakePaperclip = GetButtonEnabled(gameData, "btnMakePaperclip"),
                CanBuyAutoClipper = GetButtonEnabled(gameData, "btnMakeClipper"),
                CanBuyMegaClipper = GetButtonEnabled(gameData, "btnMakeMegaClipper"),
                CanBuyWire = GetButtonEnabled(gameData, "btnBuyWire"),
                MegaClippersUnlocked = GetButtonVisible(gameData, "btnMakeMegaClipper")
            };

            state.Business = new Business
            {
                Price = GetDouble(gameData, "price"),
                Demand = GetDouble(gameData, "demand"),
                MarketingLevel = GetInt(gameData, "marketingLevel"),
                MarketingCost = GetDouble(gameData, "marketingCost"),
                CanRaisePrice = GetButtonEnabled(gameData, "btnRaisePrice"),
                CanLowerPrice = GetButtonEnabled(gameData, "btnLowerPrice"),
                CanBuyMarketing = GetButtonEnabled(gameData, "btnExpandMarketing")
            };

            state.Computing = new Computing
            {
                Processors = GetInt(gameData, "processors"),
                Memory = GetInt(gameData, "memory"),
                Operations = GetDouble(gameData, "operations"),
                MaxOperations = GetDouble(gameData, "maxOperations"),
                Creativity = GetDouble(gameData, "creativity"),
                ProcessorCost = GetDouble(gameData, "processorCost"),
                MemoryCost = GetDouble(gameData, "memoryCost"),
                CanBuyProcessor = GetButtonEnabled(gameData, "btnAddProc"),
                CanBuyMemory = GetButtonEnabled(gameData, "btnAddMem")
            };

            state.Investments = new Investments
            {
                IsUnlocked = GetBool(gameData, "investmentsVisible"),
                PortfolioValue = GetDouble(gameData, "portfolioValue"),
                StocksValue = GetDouble(gameData, "stocksValue"),
                BondsValue = GetDouble(gameData, "bondsValue"),
                CanDeposit = GetButtonEnabled(gameData, "btnDeposit"),
                CanWithdraw = GetButtonEnabled(gameData, "btnWithdraw")
            };

            state.Space = new SpacePhase
            {
                IsUnlocked = GetBool(gameData, "spaceVisible"),
                ProbeCount = GetLong(gameData, "probes"),
                ExplorationPercent = GetDouble(gameData, "exploration") / 100.0,
                Matter = GetLong(gameData, "matter"),
                AcquiredMatter = GetLong(gameData, "acquiredMatter"),
                HarvesterDrones = GetLong(gameData, "harvesterDrones"),
                WireDrones = GetLong(gameData, "wireDrones"),
                Combat = new CombatStats
                {
                    Honor = GetLong(gameData, "honor")
                }
            };

            // Parse projects
            if (gameData.TryGetValue("projects", out var projectsObj) && projectsObj is System.Text.Json.JsonElement projectsJson)
            {
                foreach (var proj in projectsJson.EnumerateArray())
                {
                    state.Projects.Available.Add(new Project
                    {
                        Id = proj.GetProperty("id").GetString() ?? "",
                        Title = proj.GetProperty("title").GetString() ?? "",
                        Cost = proj.GetProperty("cost").GetString() ?? "",
                        IsAvailable = proj.GetProperty("available").GetBoolean()
                    });
                }
            }

            // Determine game phase
            state.Phase = DeterminePhase(state);

            // Build available actions list
            state.AvailableActions = BuildAvailableActions(state, gameData);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error capturing game state");
        }

        return state;
    }

    /// <summary>
    /// Determines the current game phase based on unlocked features.
    /// </summary>
    private static GamePhase DeterminePhase(GameStateSnapshot state)
    {
        if (state.Space.IsUnlocked && state.Space.ProbeCount > 0)
            return GamePhase.Late;

        if (state.Investments.IsUnlocked || state.Manufacturing.MegaClippersUnlocked)
            return GamePhase.Middle;

        return GamePhase.Early;
    }

    private List<string> BuildAvailableActions(GameStateSnapshot state, Dictionary<string, object?> gameData)
    {
        var actions = new List<string>();

        // Basic actions
        if (state.Manufacturing.CanMakePaperclip) actions.Add("MakePaperclip");
        if (state.Manufacturing.CanBuyWire) actions.Add("BuyWire");
        if (state.Business.CanRaisePrice) actions.Add("RaisePrice");
        if (state.Business.CanLowerPrice) actions.Add("LowerPrice");
        if (state.Business.CanBuyMarketing) actions.Add("BuyMarketing");
        if (state.Manufacturing.CanBuyAutoClipper) actions.Add("BuyAutoClipper");
        if (state.Manufacturing.CanBuyMegaClipper) actions.Add("BuyMegaClipper");
        if (state.Computing.CanBuyProcessor) actions.Add("BuyProcessor");
        if (state.Computing.CanBuyMemory) actions.Add("BuyMemory");

        // Investment actions
        if (state.Investments.CanDeposit) actions.Add("Deposit");
        if (state.Investments.CanWithdraw) actions.Add("Withdraw");

        // Project actions
        foreach (var project in state.Projects.Available.Where(p => p.IsAvailable))
        {
            actions.Add($"ActivateProject:{project.Id}");
        }

        // Space actions
        if (GetButtonEnabled(gameData, "btnMakeProbe")) actions.Add("LaunchProbe");

        return actions;
    }

    private static long GetLong(Dictionary<string, object?> data, string key)
    {
        if (!data.TryGetValue(key, out var val) || val == null)
            return 0;
        if (val is System.Text.Json.JsonElement elem)
            return elem.ValueKind == System.Text.Json.JsonValueKind.Number ? (long)elem.GetDouble() : 0;
        try { return Convert.ToInt64(val); } catch { return 0; }
    }

    private static int GetInt(Dictionary<string, object?> data, string key)
    {
        if (!data.TryGetValue(key, out var val) || val == null)
            return 0;
        if (val is System.Text.Json.JsonElement elem)
            return elem.ValueKind == System.Text.Json.JsonValueKind.Number ? (int)elem.GetDouble() : 0;
        try { return Convert.ToInt32(val); } catch { return 0; }
    }

    private static double GetDouble(Dictionary<string, object?> data, string key)
    {
        if (!data.TryGetValue(key, out var val) || val == null)
            return 0;
        if (val is System.Text.Json.JsonElement elem)
            return elem.ValueKind == System.Text.Json.JsonValueKind.Number ? elem.GetDouble() : 0;
        try { return Convert.ToDouble(val); } catch { return 0; }
    }

    private static bool GetBool(Dictionary<string, object?> data, string key)
    {
        if (!data.TryGetValue(key, out var val) || val == null)
            return false;
        if (val is System.Text.Json.JsonElement elem)
        {
            return elem.ValueKind switch
            {
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                _ => false
            };
        }
        try { return Convert.ToBoolean(val); } catch { return false; }
    }

    private static bool GetButtonEnabled(Dictionary<string, object?> data, string key)
    {
        if (!data.TryGetValue(key, out var val) || val is not System.Text.Json.JsonElement elem)
            return false;
        return elem.TryGetProperty("enabled", out var enabled) && enabled.GetBoolean();
    }

    private static bool GetButtonVisible(Dictionary<string, object?> data, string key)
    {
        if (!data.TryGetValue(key, out var val) || val is not System.Text.Json.JsonElement elem)
            return false;
        return elem.TryGetProperty("visible", out var visible) && visible.GetBoolean();
    }
}
