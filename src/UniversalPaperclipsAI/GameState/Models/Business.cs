namespace UniversalPaperclipsAI.GameState.Models;

public class Business
{
    public double Price { get; set; }
    public double Demand { get; set; }
    public int MarketingLevel { get; set; }
    public double MarketingCost { get; set; }
    public double DemandMultiplier { get; set; }
    public bool CanRaisePrice { get; set; }
    public bool CanLowerPrice { get; set; }
    public bool CanBuyMarketing { get; set; }

    public override string ToString() =>
        $"Price: ${Price:N2} | Demand: {Demand:P0} | Marketing Lvl: {MarketingLevel}";
}
