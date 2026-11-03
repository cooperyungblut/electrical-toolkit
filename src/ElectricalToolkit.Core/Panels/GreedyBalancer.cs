namespace ElectricalToolkit.Core.Panels;

public sealed class GreedyBalancer : IPanelBalancer
{
    public BalanceResult Balance(PanelConfiguration panel, IReadOnlyList<Load> loads)
    {
        LoadValidation.Validate(panel, loads);
        return ResultBuilder.Build(panel, Assign(panel, loads));
    }

    // Stage 1: pick phases for every load. Pinned loads first, since they can't move.
    // Then biggest breakers first: 3-pole loads only use up slots,
    // 2-pole loads need room on two phases, and 1-pole loads fill in what's left.
    // Within each size, largest VA first, onto the option that raises the objective least.
    // For a 1-pole load that's simply the lightest phase with an open slot.
    internal static List<(Load Load, IReadOnlyList<Phase> Phases)> Assign(PanelConfiguration panel, IReadOnlyList<Load> loads)
    {
        var ledger = new PhaseLedger(panel);
        var assignments = new List<(Load, IReadOnlyList<Phase>)>();
        foreach (var load in loads.OrderByDescending(l => l.IsPinned).ThenByDescending(l => l.Poles).ThenByDescending(l => l.Va))
        {
            var phases = PhaseOptions.For(panel, load)
                .Where(ledger.Fits)
                .MinBy(option => ledger.AddDelta(load, option))
                ?? throw new ArgumentException($"{load.Name}: no phases have open slots for it. Add spaces to the panel.");
            ledger.Add(load, phases);
            assignments.Add((load, phases));
        }
        return assignments;
    }
}
