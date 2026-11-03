namespace ElectricalToolkit.Core.Panels;

// Running VA total and slot count for each phase while a balancer tries assignments.
// Each pole of a load adds the load's VA per pole to its phase and uses one slot there.
internal sealed class PhaseLedger
{
    private readonly Dictionary<Phase, double> _totals;
    private readonly Dictionary<Phase, int> _used;
    private readonly Dictionary<Phase, int> _capacity;

    public PhaseLedger(PanelConfiguration panel)
    {
        _totals = panel.Phases.ToDictionary(p => p, _ => 0.0);
        _used = panel.Phases.ToDictionary(p => p, _ => 0);
        _capacity = panel.Phases.ToDictionary(p => p, p => panel.SlotsOnPhase(p).Count());
    }

    public IReadOnlyDictionary<Phase, double> Totals => _totals;

    public bool Fits(IReadOnlyList<Phase> phases) => phases.All(p => _used[p] < _capacity[p]);

    // Moving a load only needs room on the phases it doesn't already use.
    public bool FitsMove(IReadOnlyList<Phase> from, IReadOnlyList<Phase> to) =>
        to.Where(p => !from.Contains(p)).All(p => _used[p] < _capacity[p]);

    public void Add(Load load, IReadOnlyList<Phase> phases)
    {
        foreach (var p in phases)
        {
            _totals[p] += load.VaPerPole;
            _used[p]++;
        }
    }

    public void Remove(Load load, IReadOnlyList<Phase> phases)
    {
        foreach (var p in phases)
        {
            _totals[p] -= load.VaPerPole;
            _used[p]--;
        }
    }

    public void Move(Load load, IReadOnlyList<Phase> from, IReadOnlyList<Phase> to)
    {
        Remove(load, from);
        Add(load, to);
    }

    // How much the objective (sum of squared phase totals) would change. Negative is better.
    // Worked out from the changes alone, (t + d)^2 - t^2 = 2td + d^2, so it's cheap and has
    // no rounding error from subtracting two large sums.
    public double Delta(IEnumerable<(Load Load, IReadOnlyList<Phase> From, IReadOnlyList<Phase> To)> moves)
    {
        var change = new Dictionary<Phase, double>();
        foreach (var (load, from, to) in moves)
        {
            foreach (var p in from)
            {
                change[p] = change.GetValueOrDefault(p) - load.VaPerPole;
            }
            foreach (var p in to)
            {
                change[p] = change.GetValueOrDefault(p) + load.VaPerPole;
            }
        }
        return change.Sum(kv => 2 * _totals[kv.Key] * kv.Value + kv.Value * kv.Value);
    }

    public double AddDelta(Load load, IReadOnlyList<Phase> phases) => Delta([(load, [], phases)]);
}
