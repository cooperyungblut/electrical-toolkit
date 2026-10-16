namespace ElectricalToolkit.Core.Panels;

public sealed record Load(string Name, double Va, int Poles = 1)
{
    public double VaPerPole => Va / Poles;
}

public sealed record Placement(Load Load, IReadOnlyList<int> Slots, IReadOnlyList<Phase> Phases);

public sealed record BalanceResult(
    IReadOnlyList<Placement> Placements,
    IReadOnlyDictionary<Phase, double> PhaseTotals,
    double ImbalancePercent);

public interface IPanelBalancer
{
    BalanceResult Balance(PanelConfiguration panel, IReadOnlyList<Load> loads);
}