using ElectricalToolkit.Core.Panels;

namespace ElectricalToolkit.Core.Tests.Panels;

public class BruteForceBalancerTests
{
    private static readonly PanelConfiguration Panel42 = new(PanelSystem.ThreePhase208Y120, 42);
    private readonly BruteForceBalancer _balancer = new();

    private static List<Load> Loads(params double[] vas) =>
        vas.Select((va, i) => new Load($"L{i + 1}", va)).ToList();

    [Fact]
    public void Finds_the_perfect_split_greedy_misses()
    {
        var result = _balancer.Balance(Panel42, Loads(6000, 3000, 3000, 2000, 2000, 2000));
        Assert.All(result.PhaseTotals.Values, t => Assert.Equal(6000, t, precision: 6));
    }

    [Fact]
    public void Respects_slot_capacity_on_a_small_panel()
    {
        // 6 spaces = 2 slots per phase. Ignoring capacity, the best split would put the
        // big load alone and stack all five small ones on the other two phases.
        var panel6 = new PanelConfiguration(PanelSystem.ThreePhase208Y120, 6);
        var result = _balancer.Balance(panel6, Loads(10_000, 100, 100, 100, 100, 100));
        Assert.All(panel6.Phases, p => Assert.Equal(2, result.Placements.Count(x => x.Phases[0] == p)));
    }

    [Fact]
    public void Every_load_is_placed_exactly_once_on_a_slot_matching_its_phase()
    {
        var loads = Loads(1800, 1500, 1200, 900, 720, 500, 180);
        var result = _balancer.Balance(Panel42, loads);
        Assert.Equal(loads.Count, result.Placements.Count);
        Assert.Equal(loads.Count, result.Placements.SelectMany(p => p.Slots).Distinct().Count());
        Assert.All(result.Placements, p => Assert.Equal(Panel42.PhaseOfSlot(p.Slots[0]), p.Phases[0]));
    }

    [Fact]
    public void Too_many_loads_for_brute_force_is_rejected() =>
        Assert.Throws<ArgumentException>(() =>
            _balancer.Balance(Panel42, Loads(Enumerable.Repeat(100.0, BruteForceBalancer.MaxLoads + 1).ToArray())));
}