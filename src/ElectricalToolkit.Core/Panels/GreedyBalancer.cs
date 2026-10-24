namespace ElectricalToolkit.Core.Panels;

public sealed class GreedyBalancer : IPanelBalancer
{
    public BalanceResult Balance(PanelConfiguration panel, IReadOnlyList<Load> loads)
    {
        LoadValidation.Validate(panel, loads);

        var sortedLoads = loads.OrderByDescending(load => load.Va);
        var placements = new List<Placement>();
        var totals = panel.Phases.ToDictionary(p => p, _ => 0.0);
        var freeSlots = panel.Phases.ToDictionary(p => p, p => new Queue<int>(panel.SlotsOnPhase(p)));
        foreach (var load in sortedLoads)
        {
            Phase target = panel.Phases.Where(p => freeSlots[p].Count > 0).MinBy(p => totals[p]);
            totals[target] += load.Va;
            placements.Add(new Placement(load, [freeSlots[target].Dequeue()], [target]));
        }
        BalanceResult result = new BalanceResult(placements, totals, Imbalance.Percent(totals.Values));
        return result; 
    }
}