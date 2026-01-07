namespace UniversalPaperclipsAI.GameState.Models;

public class Manufacturing
{
    public double ClipsPerSecond { get; set; }
    public int AutoClippers { get; set; }
    public int MegaClippers { get; set; }
    public double AutoClipperCost { get; set; }
    public double MegaClipperCost { get; set; }
    public double WireCost { get; set; }
    public bool CanMakePaperclip { get; set; }
    public bool CanBuyAutoClipper { get; set; }
    public bool CanBuyMegaClipper { get; set; }
    public bool CanBuyWire { get; set; }
    public bool MegaClippersUnlocked { get; set; }

    public override string ToString() =>
        $"Rate: {ClipsPerSecond:N1}/s | AutoClippers: {AutoClippers} | MegaClippers: {MegaClippers}";
}
