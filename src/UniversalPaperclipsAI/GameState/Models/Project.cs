namespace UniversalPaperclipsAI.GameState.Models;

public class Project
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Cost { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public ProjectCostType CostType { get; set; }
    public double CostAmount { get; set; }

    public override string ToString() =>
        $"[{Id}] {Title} - {Cost}";
}

public enum ProjectCostType
{
    Operations,
    Creativity,
    Yomi,
    Trust,
    Honor,
    Unknown
}

public class ProjectList
{
    public List<Project> Available { get; set; } = new();
    public int CompletedCount { get; set; }

    public override string ToString() =>
        $"Available Projects: {Available.Count} | Completed: {CompletedCount}";
}
