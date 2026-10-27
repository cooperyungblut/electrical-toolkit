using ElectricalToolkit.Core.Panels;

namespace ElectricalToolkit.Core.Tests.Panels;

public class LocalSearchBalancerTests
{
    private static readonly PanelConfiguration Panel42 = new(PanelSystem.ThreePhase208Y120, 42);
    private readonly LocalSearchBalancer _balancer = new();

    private static List<Load> Loads(params double[] vas) =>
        vas.Select((va, i) => new Load($"L{i + 1}", va)).ToList();

    [Fact]
    public void Fixes_the_case_greedy_gets_wrong()
    {
        var result = _balancer.Balance(Panel42, Loads(6000, 3000, 3000, 2000, 2000, 2000));
        Assert.All(result.PhaseTotals.Values, t => Assert.Equal(6000, t, precision: 6));
        Assert.Equal(0, result.ImbalancePercent, precision: 6);
    }

    [Fact]
    public void No_loads_gives_zero_totals()
    {
        var result = _balancer.Balance(Panel42, []);
        Assert.All(result.PhaseTotals.Values, t => Assert.Equal(0, t));
    }

    [Fact]
    public void Full_panel_still_respects_phase_slot_capacity()
    {
        var loads = Loads([100_000, .. Enumerable.Repeat(100.0, 41)]);
        var result = _balancer.Balance(Panel42, loads);
        Assert.All(Panel42.Phases, p => Assert.Equal(14, result.Placements.Count(x => x.Phases[0] == p)));
    }

    [Fact]
    public void Same_input_gives_the_same_output()
    {
        var loads = Loads(1800, 1500, 1200, 900, 720, 500, 180, 1500);
        var first = _balancer.Balance(Panel42, loads);
        var second = _balancer.Balance(Panel42, loads);
        // Placement holds lists, and lists compare by reference, so compare the contents.
        Assert.Equal(
            first.Placements.Select(p => (p.Load, p.Slots[0], p.Phases[0])),
            second.Placements.Select(p => (p.Load, p.Slots[0], p.Phases[0])));
    }

    [Fact]
    public void Invalid_input_is_rejected_the_same_way_as_greedy()
    {
        Assert.Throws<ArgumentException>(() => _balancer.Balance(Panel42, [new Load("", 1000)]));
        Assert.Throws<NotSupportedException>(() => _balancer.Balance(Panel42, [new Load("Pump", 1000, 2)]));
    }
}