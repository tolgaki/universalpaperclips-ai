# Universal Paperclips AI Controller

An autonomous AI system that plays [Universal Paperclips](https://www.decisionproblem.com/paperclips/index2.html) using OpenAI's GPT-4. Watch as an LLM makes strategic decisions to convert the entire universe into paperclips.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![License](https://img.shields.io/badge/License-MIT-green)
![Playwright](https://img.shields.io/badge/Playwright-1.49-45ba4b)

## Overview

This project demonstrates LLM decision-making capabilities in a complex, multi-phase strategy game. The AI controller:

- **Reads game state** via browser automation (Playwright)
- **Makes decisions** using OpenAI GPT-4 with game-specific prompting
- **Executes actions** through DOM manipulation
- **Displays progress** via rich console UI and in-browser overlay

## Features

- Full autonomous gameplay across all 3 game phases
- Event-driven decision making (triggers on significant game events)
- Real-time Spectre.Console dashboard with game statistics
- In-browser overlay showing AI reasoning and actions
- Configurable decision intervals and display options
- Comprehensive action support (25+ game actions)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [OpenAI API Key](https://platform.openai.com/api-keys)

## Quick Start

```bash
# Clone the repository
git clone https://github.com/yourusername/universalpaperclips-ai.git
cd universalpaperclips-ai

# Set your OpenAI API key
export OPENAI_API_KEY="sk-..."

# Install Playwright browsers (first time only)
cd src/UniversalPaperclipsAI
dotnet build
./.playwright/node/*/node ./.playwright/package/cli.js install chromium

# Run the AI controller
dotnet run
```

## Configuration

Edit `src/UniversalPaperclipsAI/appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "",              // Or use OPENAI_API_KEY env var
    "Model": "gpt-4-turbo",    // gpt-4o, gpt-4-turbo, etc.
    "MaxTokens": 2000
  },
  "Game": {
    "Url": "https://www.decisionproblem.com/paperclips/index2.html",
    "PollIntervalMs": 500,     // How often to read game state
    "DecisionIntervalMs": 3000, // Minimum time between LLM calls
    "MaxActionsPerDecision": 5
  },
  "Display": {
    "ShowBrowser": true,       // Show Chrome window
    "ShowConsole": true,       // Show console dashboard
    "OverlayEnabled": true     // Show in-game AI overlay
  }
}
```

## Game Phases

The AI handles all three phases of Universal Paperclips:

### Phase 1: Early Game
- Manual paperclip creation
- Price optimization
- Wire management
- Trust allocation (processors vs memory)
- First AutoClippers

### Phase 2: Middle Game
- AutoClipper and MegaClipper scaling
- Investment engine management
- Marketing optimization
- Quantum computing projects
- Strategic modeling (Yomi)

### Phase 3: Space Expansion
- Probe design and launch
- Drone management (harvester/wire)
- Matter acquisition
- Combat encounters
- Universe conversion

## Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed design documentation.

```
UniversalPaperclipsAI/
├── Browser/           # Playwright browser automation
├── GameState/         # Game state extraction and models
├── Actions/           # Action definitions and execution
├── AI/                # OpenAI integration and prompting
├── Reporting/         # Console and browser UI
└── Core/              # Main loop and configuration
```

## How It Works

1. **State Observation**: JavaScript injection extracts all game values from the DOM
2. **Event Detection**: Triggers LLM when projects unlock, resources change, or phases transition
3. **Decision Making**: GPT-4 receives game state and returns JSON with reasoning + actions
4. **Action Execution**: Playwright clicks buttons and adjusts sliders
5. **Display**: Real-time updates to console dashboard and browser overlay

## Supported Actions

| Category | Actions |
|----------|---------|
| Manufacturing | MakePaperclip, BuyWire, BuyAutoClipper, BuyMegaClipper |
| Business | RaisePrice, LowerPrice, BuyMarketing |
| Computing | BuyProcessor, BuyMemory, QuantumCompute |
| Investment | Deposit, Withdraw, SetLowRisk, SetMediumRisk, SetHighRisk |
| Projects | ActivateProject:{id} |
| Space | LaunchProbe, MakeFactory, MakeHarvester, MakeWireDrone |
| Other | RunTournament, Entertain, Synchronize |

## Development

```bash
# Build
dotnet build

# Run with verbose logging
dotnet run -- --verbose

# Run tests (when available)
dotnet test
```

## Cost Considerations

Each decision cycle makes one OpenAI API call. With default settings (3-second intervals), expect:
- ~20 calls/minute during active play
- ~$0.01-0.03 per call with GPT-4-turbo
- Full game completion: varies based on LLM efficiency

Reduce costs by:
- Increasing `DecisionIntervalMs`
- Using `gpt-4o-mini` for experimentation
- Running in headless mode for faster execution

## Troubleshooting

### Playwright browser not found
```bash
cd src/UniversalPaperclipsAI/bin/Debug/net10.0
./.playwright/node/*/node ./.playwright/package/cli.js install chromium
```

### OpenAI API errors
- Verify your API key is set correctly
- Check your OpenAI account has available credits
- Ensure the model name is valid

### Game not loading
- Check your internet connection
- Verify the game URL is accessible
- Try increasing the page load timeout

## Contributing

Contributions are welcome! Please read the issues list for known improvements needed.

## License

MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

- [Universal Paperclips](https://www.decisionproblem.com/paperclips/) by Frank Lantz
- [Playwright](https://playwright.dev/) for browser automation
- [Spectre.Console](https://spectreconsole.net/) for terminal UI
- [OpenAI](https://openai.com/) for GPT-4 API
