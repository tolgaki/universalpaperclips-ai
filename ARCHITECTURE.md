# Architecture

This document describes the design and architecture of the Universal Paperclips AI Controller.

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Universal Paperclips AI                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────┐     ┌──────────────┐     ┌────────────────────────┐   │
│  │   Browser    │────▶│  Game State  │────▶│   Decision Engine      │   │
│  │  Controller  │     │   Observer   │     │   (OpenAI GPT-4)       │   │
│  │ (Playwright) │     │              │     │                        │   │
│  └──────────────┘     └──────────────┘     └────────────────────────┘   │
│         │                    │                        │                  │
│         │                    ▼                        ▼                  │
│         │            ┌──────────────┐     ┌────────────────────────┐    │
│         │            │    Event     │     │    Action Executor     │    │
│         │            │   Detector   │     │                        │    │
│         │            └──────────────┘     └────────────────────────┘    │
│         │                                            │                   │
│         ◀────────────────────────────────────────────┘                   │
│         │                                                                │
│         ▼                                                                │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                      Reporting Layer                              │   │
│  │   ┌─────────────────────┐    ┌──────────────────────────────┐    │   │
│  │   │  Console Renderer   │    │     Browser Overlay          │    │   │
│  │   │  (Spectre.Console)  │    │     (DOM Injection)          │    │   │
│  │   └─────────────────────┘    └──────────────────────────────┘    │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

## Component Details

### 1. Browser Controller (`Browser/BrowserController.cs`)

**Responsibility**: Manages the Playwright browser instance and provides low-level DOM interaction.

**Key Features**:
- Launches Chromium with configurable headless/visible mode
- Navigates to game URL and waits for initialization
- Provides click, slider, and JavaScript evaluation methods
- Handles browser lifecycle and cleanup via `IAsyncDisposable`

**Design Decisions**:
- Uses Playwright over Selenium for modern async API and better reliability
- Exposes `IPage` for direct access when needed
- Silent failure for click operations (game may disable buttons between state read and action)

### 2. Game State Observer (`GameState/GameStateObserver.cs`)

**Responsibility**: Extracts complete game state from the DOM in a single JavaScript evaluation.

**Key Features**:
- Single JS call extracts all values (minimizes round-trips)
- Parses numeric values with locale-agnostic regex
- Detects button enabled/disabled states
- Extracts dynamic project list

**State Model Hierarchy**:
```
GameStateSnapshot
├── Resources (clips, funds, wire, trust, operations, creativity, yomi)
├── Manufacturing (rate, autoclippers, megaclippers, costs)
├── Business (price, demand, marketing)
├── Computing (processors, memory, operations)
├── Projects (available projects with costs)
├── Investments (portfolio, stocks, bonds)
└── Space (probes, exploration, drones, matter)
```

### 3. Event Detector (`GameState/EventDetector.cs`)

**Responsibility**: Determines when to trigger an LLM decision based on game state changes.

**Trigger Conditions**:
1. New project becomes available
2. Game phase transitions (Early → Middle → Late)
3. Significant resource thresholds crossed
4. New actions become available
5. Fallback: time-based interval (configurable)

**Design Rationale**: Event-driven approach reduces API costs compared to fixed-interval polling while ensuring responsive gameplay.

### 4. Decision Engine (`AI/DecisionEngine.cs`)

**Responsibility**: Interfaces with OpenAI API to get strategic decisions.

**Key Features**:
- Builds context-aware prompts with current game state
- Parses JSON responses with markdown code block stripping
- Maintains decision history for debugging/analysis
- Provides safe fallback actions on API errors

**Prompt Structure**:
```
System Prompt (SystemPrompt.cs):
├── Game mechanics explanation
├── Phase-specific strategies
├── Available actions documentation
└── Response format specification

User Prompt (PromptBuilder.cs):
├── Current game state (all resources/metrics)
├── Available actions list
├── Recent action history
└── Decision request
```

**Response Format**:
```json
{
  "reasoning": "Strategic explanation...",
  "actions": [
    {"type": "ActionName", "parameters": {}}
  ],
  "priority": "Current focus area"
}
```

### 5. Action Executor (`Actions/ActionExecutor.cs`)

**Responsibility**: Translates LLM decisions into browser interactions.

**Action Types**:
- **Click Actions**: Single or multi-click (e.g., MakePaperclip10)
- **Slider Actions**: Set value and dispatch input event
- **Project Actions**: Dynamic selector based on project ID

**Action Registry** (`GameActions.cs`):
- Static dictionary of action definitions
- Maps action names to CSS selectors
- Includes click count and slider configurations

### 6. Game Loop (`Core/GameLoop.cs`)

**Responsibility**: Orchestrates the main control flow.

**Loop Structure**:
```
while running:
    1. Capture game state
    2. Check if decision needed (EventDetector)
    3. If yes:
       a. Call DecisionEngine
       b. Execute actions
       c. Update overlay
    4. Update console display
    5. Wait for poll interval
```

### 7. Reporting Layer

#### Console Renderer (`Reporting/ConsoleRenderer.cs`)
- Uses Spectre.Console for rich terminal UI
- Four-panel layout: Resources, Manufacturing, AI Decision, Action History
- Thread-safe rendering with lock

#### Browser Overlay (`Reporting/BrowserOverlay.cs`)
- Injects floating CSS/HTML panel into game page
- Shows AI status, current priority, reasoning, and recent actions
- Updates via JavaScript DOM manipulation

## Data Flow

```
┌─────────┐    JavaScript     ┌─────────────┐
│  Game   │ ◀──────────────── │  Observer   │
│   DOM   │    Evaluation     │             │
└─────────┘                   └──────┬──────┘
     ▲                               │
     │                               ▼
     │                        ┌─────────────┐
     │ Click/                 │   Event     │
     │ Slider                 │  Detector   │
     │                        └──────┬──────┘
     │                               │ Trigger?
     │                               ▼
┌─────────┐                   ┌─────────────┐
│ Action  │ ◀──────────────── │  Decision   │
│Executor │    Actions        │   Engine    │
└─────────┘                   └──────┬──────┘
                                     │
                                     ▼
                              ┌─────────────┐
                              │   OpenAI    │
                              │    API      │
                              └─────────────┘
```

## Configuration

Configuration is loaded from `appsettings.json` and environment variables:

| Setting | Default | Description |
|---------|---------|-------------|
| OpenAI.Model | gpt-4-turbo | Model for decision making |
| OpenAI.MaxTokens | 2000 | Max response tokens |
| Game.PollIntervalMs | 500 | State capture frequency |
| Game.DecisionIntervalMs | 3000 | Minimum time between decisions |
| Display.ShowBrowser | true | Show Chrome window |
| Display.OverlayEnabled | true | Show in-game overlay |

## Error Handling Strategy

1. **Browser Errors**: Fail fast with clear error message
2. **State Extraction**: Return partial state, log errors
3. **API Errors**: Return safe default actions, continue loop
4. **Action Errors**: Log and continue (silent failure for disabled buttons)

## Extension Points

### Adding New Actions
1. Add entry to `GameActions.All` dictionary
2. Include in `SystemPrompt.cs` action documentation
3. Add to `BuildAvailableActions()` if conditionally available

### Adding New Game State
1. Add property to appropriate model in `GameState/Models/`
2. Extract value in `GameStateObserver.CaptureStateAsync()`
3. Include in `PromptBuilder.BuildDecisionPrompt()`

### Custom LLM Providers
Replace `DecisionEngine` OpenAI calls with alternative provider while maintaining `LLMDecision` response format.

## Performance Considerations

- **API Costs**: Event-driven triggers minimize unnecessary calls
- **Browser Memory**: Single page, no navigation
- **State Extraction**: Single JS evaluation per tick
- **Console Rendering**: Lock prevents race conditions

## Security Notes

- API key loaded from environment variable (preferred) or config file
- No sensitive data logged to console
- Browser runs with automation detection disabled
