namespace UniversalPaperclipsAI.GameState.Models;

public class Computing
{
    public int Processors { get; set; }
    public int Memory { get; set; }
    public double Operations { get; set; }
    public double MaxOperations { get; set; }
    public double Creativity { get; set; }
    public double ProcessorCost { get; set; }
    public double MemoryCost { get; set; }
    public bool CanBuyProcessor { get; set; }
    public bool CanBuyMemory { get; set; }
    public bool QuantumComputingUnlocked { get; set; }
    public int QuantumChips { get; set; }

    public override string ToString() =>
        $"Processors: {Processors} | Memory: {Memory} | Ops: {Operations:N0}/{MaxOperations:N0} | Creativity: {Creativity:N0}";
}
