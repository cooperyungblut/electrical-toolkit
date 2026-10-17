namespace ElectricalToolkit.Core.Panels;

public sealed class GreedyBalancer : IPanelBalancer
{
    public BalanceResult Balance(PanelConfiguration panel, IReadOnlyList<Load> loads)
    {
        if (loads.Count > panel.Spaces) throw new ArgumentException($"Too many loads", nameof(loads));

        var sortedLoads = loads.OrderByDescending(load => load.Va);
        List<Placement> placements = new List<Placement>();
        var totals = panel.Phases.ToDictionary(p => p, _ => 0.0);
        var freeSlots = panel.Phases.ToDictionary(p => p, p => new Queue<int>(panel.SlotsOnPhase(p)));
        foreach (var load in sortedLoads)
        {
            if (string.IsNullOrWhiteSpace(load.Name)) throw new ArgumentException("Load missing a Name");
            if (load.Poles != 1) throw new NotSupportedException($"{load.Name}: Only 1-pole loads are supported in this version");
            if (load.Va <= 0) throw new ArgumentException($"{load.Name}: VA is less than or equal to 0");
            Phase target = panel.Phases.Where(p => freeSlots[p].Count > 0).MinBy(p => totals[p]);
            totals[target] += load.Va;
            placements.Add(new Placement(load, [freeSlots[target].Dequeue()], [target]));
        }
        BalanceResult result = new BalanceResult(placements, totals, Imbalance.Percent(totals.Values));
        return result; 
    }
}