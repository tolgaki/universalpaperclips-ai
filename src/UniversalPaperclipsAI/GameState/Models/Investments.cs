namespace UniversalPaperclipsAI.GameState.Models;

public class Investments
{
    public bool IsUnlocked { get; set; }
    public double PortfolioValue { get; set; }
    public double CashBalance { get; set; }
    public double StocksValue { get; set; }
    public double BondsValue { get; set; }
    public int RiskLevel { get; set; } // 1-10
    public bool CanDeposit { get; set; }
    public bool CanWithdraw { get; set; }
    public bool AutoTradingEnabled { get; set; }

    public override string ToString() =>
        IsUnlocked
            ? $"Portfolio: ${PortfolioValue:N2} | Stocks: ${StocksValue:N2} | Bonds: ${BondsValue:N2} | Risk: {RiskLevel}"
            : "Investments: Locked";
}
