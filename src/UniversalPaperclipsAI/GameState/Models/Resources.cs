namespace UniversalPaperclipsAI.GameState.Models;

public class Resources
{
    public long Paperclips { get; set; }
    public long UnsoldClips { get; set; }
    public double Funds { get; set; }
    public double Wire { get; set; }
    public int Trust { get; set; }
    public int Memory { get; set; }
    public int Processors { get; set; }
    public double Operations { get; set; }
    public double Creativity { get; set; }
    public long Yomi { get; set; }
    public long Honor { get; set; }

    public override string ToString() =>
        $"Clips: {Paperclips:N0} | Unsold: {UnsoldClips:N0} | ${Funds:N2} | Wire: {Wire:N0}\" | Trust: {Trust}";
}
