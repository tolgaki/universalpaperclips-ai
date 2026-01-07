namespace UniversalPaperclipsAI.GameState.Models;

public class SpacePhase
{
    public bool IsUnlocked { get; set; }
    public long ProbeCount { get; set; }
    public double ExplorationPercent { get; set; }
    public long Matter { get; set; }
    public long AcquiredMatter { get; set; }
    public long HarvesterDrones { get; set; }
    public long WireDrones { get; set; }
    public long ClipFactories { get; set; }
    public ProbeDesign CurrentProbeDesign { get; set; } = new();
    public CombatStats Combat { get; set; } = new();
    public SwarmComputing Swarm { get; set; } = new();

    public override string ToString() =>
        IsUnlocked
            ? $"Probes: {ProbeCount:N0} | Explored: {ExplorationPercent:P2} | Matter: {Matter:N0}"
            : "Space Phase: Locked";
}

public class ProbeDesign
{
    public int Speed { get; set; }
    public int Exploration { get; set; }
    public int SelfReplication { get; set; }
    public int HazardRemediation { get; set; }
    public int FactoryProduction { get; set; }
    public int HarvesterDroneProduction { get; set; }
    public int WireDroneProduction { get; set; }
    public int Combat { get; set; }

    public int TotalPoints => Speed + Exploration + SelfReplication + HazardRemediation +
                              FactoryProduction + HarvesterDroneProduction + WireDroneProduction + Combat;
}

public class CombatStats
{
    public long Honor { get; set; }
    public int DrifterEncounters { get; set; }
    public int DriftersDefeated { get; set; }
    public bool InCombat { get; set; }
}

public class SwarmComputing
{
    public bool IsUnlocked { get; set; }
    public double Entertainment { get; set; }
    public double Synchronization { get; set; }
    public bool CanEntertain { get; set; }
    public bool CanSynchronize { get; set; }
}
