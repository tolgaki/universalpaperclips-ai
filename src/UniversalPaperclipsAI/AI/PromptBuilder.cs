using System.Text;
using System.Text.Json;
using UniversalPaperclipsAI.Actions;
using UniversalPaperclipsAI.GameState;

namespace UniversalPaperclipsAI.AI;

public class PromptBuilder
{
    public string BuildDecisionPrompt(GameStateSnapshot state, IEnumerable<ActionLogEntry> recentActions)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## CURRENT GAME STATE");
        sb.AppendLine();
        sb.AppendLine($"**Phase:** {state.Phase}");
        sb.AppendLine($"**Time:** {state.Timestamp:HH:mm:ss}");
        sb.AppendLine();

        // Resources
        sb.AppendLine("### Resources");
        sb.AppendLine($"- Paperclips Made: {state.Resources.Paperclips:N0}");
        sb.AppendLine($"- Unsold Inventory: {state.Resources.UnsoldClips:N0}");
        sb.AppendLine($"- Funds: ${state.Resources.Funds:N2}");
        sb.AppendLine($"- Wire: {state.Resources.Wire:N0} inches");
        sb.AppendLine($"- Trust: {state.Resources.Trust}");
        sb.AppendLine();

        // Manufacturing
        sb.AppendLine("### Manufacturing");
        sb.AppendLine($"- Production Rate: {state.Manufacturing.ClipsPerSecond:N1} clips/sec");
        sb.AppendLine($"- AutoClippers: {state.Manufacturing.AutoClippers} (Cost: ${state.Manufacturing.AutoClipperCost:N2})");
        if (state.Manufacturing.MegaClippersUnlocked)
            sb.AppendLine($"- MegaClippers: {state.Manufacturing.MegaClippers} (Cost: ${state.Manufacturing.MegaClipperCost:N2})");
        sb.AppendLine($"- Wire Cost: ${state.Manufacturing.WireCost:N2}");
        sb.AppendLine();

        // Business
        sb.AppendLine("### Business");
        sb.AppendLine($"- Price: ${state.Business.Price:N2}");
        sb.AppendLine($"- Public Demand: {state.Business.Demand:P0}");
        sb.AppendLine($"- Marketing Level: {state.Business.MarketingLevel} (Next: ${state.Business.MarketingCost:N0})");
        sb.AppendLine();

        // Computing
        sb.AppendLine("### Computing");
        sb.AppendLine($"- Processors: {state.Computing.Processors}");
        sb.AppendLine($"- Memory: {state.Computing.Memory}");
        sb.AppendLine($"- Operations: {state.Computing.Operations:N0} / {state.Computing.MaxOperations:N0}");
        if (state.Computing.Creativity > 0)
            sb.AppendLine($"- Creativity: {state.Computing.Creativity:N0}");
        sb.AppendLine();

        // Investments (if unlocked)
        if (state.Investments.IsUnlocked)
        {
            sb.AppendLine("### Investments");
            sb.AppendLine($"- Portfolio Value: ${state.Investments.PortfolioValue:N2}");
            sb.AppendLine($"- Stocks: ${state.Investments.StocksValue:N2}");
            sb.AppendLine($"- Bonds: ${state.Investments.BondsValue:N2}");
            sb.AppendLine();
        }

        // Space (if unlocked)
        if (state.Space.IsUnlocked)
        {
            sb.AppendLine("### Space Exploration");
            sb.AppendLine($"- Probes: {state.Space.ProbeCount:N0}");
            sb.AppendLine($"- Universe Explored: {state.Space.ExplorationPercent:P2}");
            sb.AppendLine($"- Available Matter: {state.Space.Matter:N0}");
            sb.AppendLine($"- Harvester Drones: {state.Space.HarvesterDrones:N0}");
            sb.AppendLine($"- Wire Drones: {state.Space.WireDrones:N0}");
            if (state.Space.Combat.Honor > 0)
                sb.AppendLine($"- Honor: {state.Space.Combat.Honor:N0}");
            sb.AppendLine();
        }

        // Available Projects
        if (state.Projects.Available.Any())
        {
            sb.AppendLine("### Available Projects");
            foreach (var project in state.Projects.Available.Where(p => p.IsAvailable).Take(10))
            {
                sb.AppendLine($"- [{project.Id}] {project.Title} - Cost: {project.Cost}");
            }
            sb.AppendLine();
        }

        // Available Actions
        sb.AppendLine("### Available Actions");
        sb.AppendLine("```");
        foreach (var action in state.AvailableActions)
        {
            sb.AppendLine(action);
        }
        sb.AppendLine("```");
        sb.AppendLine();

        // Recent Actions (for context)
        var recent = recentActions.Take(5).ToList();
        if (recent.Any())
        {
            sb.AppendLine("### Recent Actions Taken");
            foreach (var action in recent)
            {
                sb.AppendLine($"- {action.ActionName}: {(action.Success ? "Success" : "Failed")}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("Based on the current state, what actions should be taken? Respond with valid JSON only.");

        return sb.ToString();
    }
}
