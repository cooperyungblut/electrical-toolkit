namespace ElectricalToolkit.Core.Panels;

// Turns "which phases each load is on" into slot numbers, totals and imbalance.
public static class ResultBuilder
{
    public static BalanceResult Build(PanelConfiguration panel, IEnumerable<(Load Load, IReadOnlyList<Phase> Phases)> assignments)
    {
        var placements = SlotPacker.Pack(panel, assignments.ToList());
        var totals = panel.Phases.ToDictionary(p => p, _ => 0.0);
        foreach (var placement in placements)
        {
            foreach (var phase in placement.Phases)
            {
                totals[phase] += placement.Load.VaPerPole;
            }
        }
        var amps = totals.ToDictionary(t => t.Key, t => t.Value / panel.PhaseVoltage);
        return new BalanceResult(placements, totals, Imbalance.Percent(totals.Values), amps);
    }
}
