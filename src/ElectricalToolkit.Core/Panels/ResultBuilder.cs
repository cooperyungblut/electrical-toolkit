namespace ElectricalToolkit.Core.Panels;

// Turns "which phase each load is on" into slot numbers, totals and imbalance.
public static class ResultBuilder
{
    public static BalanceResult Build(PanelConfiguration panel, IEnumerable<(Load Load, Phase Phase)> assignments)
    {
        var totals = panel.Phases.ToDictionary(p => p, _ => 0.0);
        var freeSlots = panel.Phases.ToDictionary(p => p, p => new Queue<int>(panel.SlotsOnPhase(p)));
        var placements = new List<Placement>();

        foreach (var (load, phase) in assignments)
        {
            totals[phase] += load.Va;
            placements.Add(new Placement(load, [freeSlots[phase].Dequeue()], [phase]));
        }

        return new BalanceResult(placements, totals, Imbalance.Percent(totals.Values));
    }
}