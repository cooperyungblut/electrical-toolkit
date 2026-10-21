using ElectricalToolkit.Core.Panels;

namespace ElectricalToolkit.Core.Tests.Panels;

public class GreedyBalancerTests
{
    private static readonly PanelConfiguration Panel42 = new(PanelSystem.ThreePhase208Y120, 42);
    private readonly GreedyBalancer _balancer = new();

    private static List<Load> Loads(params double[] vas) =>
        vas.Select((va, i) => new Load($"L{i + 1}", va)).ToList();

    [Fact]
    public void No_loads_gives_zero_totals_and_zero_imbalance()
    {
        var result = _balancer.Balance(Panel42, []);
        Assert.All(result.PhaseTotals.Values, t => Assert.Equal(0, t));
        Assert.Equal(0, result.ImbalancePercent);
    }

    [Fact]
    public void Three_equal_loads_land_on_three_different_phases()
    {
        var result = _balancer.Balance(Panel42, Loads(1000, 1000, 1000));
        Assert.All(result.PhaseTotals.Values, t => Assert.Equal(1000, t));
        Assert.Equal(0, result.ImbalancePercent, precision: 6);
    }

    [Fact]
    public void Single_load_is_200_percent_imbalanced()
    {
        var result = _balancer.Balance(Panel42, Loads(1000));
        Assert.Equal(200, result.ImbalancePercent, precision: 6);
    }

    [Fact]
    public void Imbalance_uses_the_largest_deviation_high_or_low()
    {
        // Average 8000. High side is +2000 (25%), low side is -4000 (50%). Low side wins.
        Assert.Equal(50, Imbalance.Percent([10_000, 10_000, 4_000]), precision: 6);
    }

    [Fact]
    public void Total_va_is_conserved()
    {
        var loads = Loads(1800, 1500, 1200, 900, 720, 500, 180);
        var result = _balancer.Balance(Panel42, loads);
        Assert.Equal(loads.Sum(l => l.Va), result.PhaseTotals.Values.Sum(), precision: 6);
    }

    [Fact]
    public void Every_load_is_placed_exactly_once_on_a_slot_matching_its_phase()
    {
        var loads = Loads(1800, 1500, 1200, 900, 720, 500, 180);
        var result = _balancer.Balance(Panel42, loads);
        Assert.Equal(loads.Count, result.Placements.Count);
        Assert.Equal(result.Placements.Count, result.Placements.SelectMany(p => p.Slots).Distinct().Count());
        Assert.All(result.Placements, p => Assert.Equal(Panel42.PhaseOfSlot(p.Slots[0]), p.Phases[0]));
    }

    [Fact]
    public void Full_panel_respects_phase_slot_capacity()
    {
        // One huge load would pull everything else onto B and C if capacity were ignored.
        var loads = Loads([100_000, .. Enumerable.Repeat(100.0, 41)]);
        var result = _balancer.Balance(Panel42, loads);
        Assert.All(Panel42.Phases, p => Assert.Equal(14, result.Placements.Count(x => x.Phases[0] == p)));
    }

    [Fact]
    public void More_loads_than_spaces_is_rejected() =>
        Assert.Throws<ArgumentException>(() => _balancer.Balance(Panel42, Loads(Enumerable.Repeat(100.0, 43).ToArray())));

    [Fact]
    public void Greedy_is_not_optimal_for_this_known_case()
    {
        // Optimal is 6000 / 6000 / 6000 (6 | 3+3 | 2+2+2). Greedy gets 7000 / 6000 / 5000.
        // Local swap improvement (milestone 2) must fix this; this test documents the weakness.
        var result = _balancer.Balance(Panel42, Loads(6000, 3000, 3000, 2000, 2000, 2000));
        Assert.Equal(7000, result.PhaseTotals.Values.Max());
        Assert.Equal(5000, result.PhaseTotals.Values.Min());
    }

    [Theory]
	[InlineData("")]
	[InlineData("   ")]
    public void Blank_Name_is_rejected(string name)
    {
        var badLoad = new Load( name, 1000);
		Assert.Throws<ArgumentException>(() => _balancer.Balance(Panel42, [badLoad]));
    }
	
	[Theory]
	[InlineData(0.0)]
	[InlineData(-100.0)]
    public void Va_is_Zero_or_less(double testVa)
    {
        var badLoad = new Load( "testName", testVa);
		Assert.Throws<ArgumentException>(() => _balancer.Balance(Panel42, [badLoad]));
    }
	
	[Theory]
	[InlineData(2)]
	[InlineData(3)]
    public void multiple_poles_not_yet_supported(int poles)
    {
        var badLoad = new Load( "testName", 1000, poles);
		Assert.Throws<NotSupportedException>(() => _balancer.Balance(Panel42, [badLoad]));
    }
}