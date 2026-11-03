namespace ElectricalToolkit.Core.Panels;

// FixedStartSlot pins the breaker where it already is (adding loads to an existing panel).
// A pinned load is counted in the phase totals but never moved.
public sealed record Load(string Name, double Va, int Poles = 1, int? FixedStartSlot = null)
{
    public double VaPerPole => Va / Poles;

    public bool IsPinned => FixedStartSlot is not null;
}

public sealed record Placement(Load Load, IReadOnlyList<int> Slots, IReadOnlyList<Phase> Phases);

public sealed record BalanceResult(
    IReadOnlyList<Placement> Placements,
    IReadOnlyDictionary<Phase, double> PhaseTotals,
    double ImbalancePercent,
    IReadOnlyDictionary<Phase, double> PhaseAmps);

public interface IPanelBalancer
{
    BalanceResult Balance(PanelConfiguration panel, IReadOnlyList<Load> loads);
}