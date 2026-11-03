using ElectricalToolkit.Core.Panels;

namespace ElectricalToolkit.Core.Tests.Panels;

// 2- and 3-pole loads, run against every balancer.
public class MultipoleTests
{
    private static readonly PanelConfiguration Panel42 = new(PanelSystem.ThreePhase208Y120, 42);
    private static readonly PanelConfiguration SinglePhase24 = new(PanelSystem.SinglePhase120240, 24);

    public static TheoryData<string> Balancers => ["Greedy", "LocalSearch", "BruteForce"];

    private static IPanelBalancer Make(string name) => name switch
    {
        "Greedy" => new GreedyBalancer(),
        "LocalSearch" => new LocalSearchBalancer(),
        "BruteForce" => new BruteForceBalancer(),
        _ => throw new ArgumentException(name),
    };

    // No shared slots, every slot inside the panel, each placement's phases match its slots,
    // and each load's slots are n, n+2, n+4.
    internal static void AssertPhysicallyValid(PanelConfiguration panel, IReadOnlyList<Load> loads, BalanceResult result)
    {
        Assert.Equal(loads.Count, result.Placements.Count);
        var allSlots = result.Placements.SelectMany(p => p.Slots).ToList();
        Assert.Equal(allSlots.Count, allSlots.Distinct().Count());
        Assert.Equal(loads.Sum(l => l.Poles), allSlots.Count);
        Assert.All(allSlots, s => Assert.InRange(s, 1, panel.Spaces));
        foreach (var p in result.Placements)
        {
            Assert.Equal(p.Load.Poles, p.Slots.Count);
            Assert.Equal(p.Slots.Select(panel.PhaseOfSlot), p.Phases);
            Assert.Equal(p.Slots.Count, p.Phases.Distinct().Count());
            for (int k = 1; k < p.Slots.Count; k++)
            {
                Assert.Equal(p.Slots[0] + 2 * k, p.Slots[k]);
            }
        }
        Assert.Equal(loads.Sum(l => l.Va), result.PhaseTotals.Values.Sum(), precision: 6);
    }

    [Theory]
    [MemberData(nameof(Balancers))]
    public void Two_pole_load_splits_its_va_across_two_different_phases(string balancer)
    {
        var loads = new List<Load> { new("Water heater", 2000, 2) };
        var result = Make(balancer).Balance(Panel42, loads);

        var placement = Assert.Single(result.Placements);
        Assert.Equal(placement.Slots[0] + 2, placement.Slots[1]);   // n and n+2, never n and n+1
        Assert.Equal(2, result.PhaseTotals.Values.Count(t => t == 1000));
        AssertPhysicallyValid(Panel42, loads, result);
    }

    [Theory]
    [MemberData(nameof(Balancers))]
    public void Three_pole_load_puts_a_third_on_each_phase(string balancer)
    {
        var loads = new List<Load> { new("Rooftop unit", 3000, 3) };
        var result = Make(balancer).Balance(Panel42, loads);

        var placement = Assert.Single(result.Placements);
        Assert.Equal([1, 3, 5], placement.Slots);
        Assert.All(result.PhaseTotals.Values, t => Assert.Equal(1000, t, precision: 6));
    }

    [Theory]
    [MemberData(nameof(Balancers))]
    public void Three_pole_loads_do_not_change_the_spread(string balancer)
    {
        List<Load> single = [new("L1", 1800), new("L2", 1200), new("L3", 500), new("L4", 400)];
        var without = Make(balancer).Balance(Panel42, single);
        var with = Make(balancer).Balance(Panel42, [.. single, new Load("RTU", 9000, 3)]);

        static double Spread(BalanceResult r) => r.PhaseTotals.Values.Max() - r.PhaseTotals.Values.Min();
        Assert.Equal(Spread(without), Spread(with), precision: 6);
    }

    [Fact]
    public void Two_pole_load_balances_against_single_pole_loads()
    {
        // 2-pole 4000 VA is 2000 on two phases. The 2000 VA load belongs on the third phase.
        List<Load> loads = [new("Dryer", 4000, 2), new("Receptacles", 2000)];
        var result = new LocalSearchBalancer().Balance(Panel42, loads);
        Assert.All(result.PhaseTotals.Values, t => Assert.Equal(2000, t, precision: 6));
    }

    [Fact]
    public void Local_search_changes_a_two_pole_phase_pair_when_that_helps()
    {
        // Greedy puts the ranges on AB then BC (A 3000, B 6000, C 3000) and the heater on A: 9000 / 6000 / 3000.
        // Moving the heater can't fix that. Moving the first range from AB to BC gives 6000 on every phase.
        List<Load> loads = [new("Range 1", 6000, 2), new("Range 2", 6000, 2), new("Heater", 6000)];
        var greedy = new GreedyBalancer().Balance(Panel42, loads);
        var local = new LocalSearchBalancer().Balance(Panel42, loads);

        Assert.Equal(9000, greedy.PhaseTotals.Values.Max(), precision: 6);
        Assert.All(local.PhaseTotals.Values, t => Assert.Equal(6000, t, precision: 6));
        AssertPhysicallyValid(Panel42, loads, local);
    }

    [Theory]
    [MemberData(nameof(Balancers))]
    public void Two_pole_on_single_phase_panel_spans_A_and_B(string balancer)
    {
        var loads = new List<Load> { new("Water heater", 4500, 2), new("Lights", 1000) };
        var result = Make(balancer).Balance(SinglePhase24, loads);

        Assert.Equal([Phase.A, Phase.B], result.Placements.Single(p => p.Load.Poles == 2).Phases.Order());
        AssertPhysicallyValid(SinglePhase24, loads, result);
    }

    [Theory]
    [MemberData(nameof(Balancers))]
    public void Three_pole_on_single_phase_panel_is_rejected(string balancer) =>
        Assert.Throws<ArgumentException>(() => Make(balancer).Balance(SinglePhase24, [new Load("Motor", 3000, 3)]));

    [Fact]
    public void More_poles_than_spaces_is_rejected() =>
        // 15 three-pole breakers need 45 spaces.
        Assert.Throws<ArgumentException>(() =>
            new GreedyBalancer().Balance(Panel42, Enumerable.Range(1, 15).Select(i => new Load($"M{i}", 1000, 3)).ToList()));

    [Fact]
    public void Full_panel_of_three_pole_breakers_packs()
    {
        var loads = Enumerable.Range(1, 14).Select(i => new Load($"M{i}", 1000 * i, 3)).ToList();
        AssertPhysicallyValid(Panel42, loads, new LocalSearchBalancer().Balance(Panel42, loads));
    }

    [Fact]
    public void Full_mixed_panel_packs()
    {
        // 7 x 3-pole + 6 x 2-pole + 9 x 1-pole = 42 spaces exactly.
        var loads = Enumerable.Range(1, 7).Select(i => new Load($"M{i}", 3000 + 100 * i, 3))
            .Concat(Enumerable.Range(1, 6).Select(i => new Load($"D{i}", 2000 + 300 * i, 2)))
            .Concat(Enumerable.Range(1, 9).Select(i => new Load($"S{i}", 200 * i)))
            .ToList();
        AssertPhysicallyValid(Panel42, loads, new LocalSearchBalancer().Balance(Panel42, loads));
    }

    [Fact]
    public void Random_mixed_panels_are_always_physically_valid_and_local_search_never_hurts()
    {
        var rng = new Random(11);
        for (int c = 0; c < 300; c++)
        {
            var loads = new List<Load>();
            int spacesLeft = 42;
            int n = rng.Next(1, 30);
            for (int i = 0; i < n; i++)
            {
                int poles = rng.Next(10) switch { < 6 => 1, < 9 => 2, _ => 3 };
                if (poles > spacesLeft) break;
                spacesLeft -= poles;
                loads.Add(new Load($"L{i + 1}", rng.Next(1, 60) * 100, poles));
            }

            var greedy = new GreedyBalancer().Balance(Panel42, loads);
            var local = new LocalSearchBalancer().Balance(Panel42, loads);
            AssertPhysicallyValid(Panel42, loads, greedy);
            AssertPhysicallyValid(Panel42, loads, local);
            Assert.True(Objective.SumOfSquares(local.PhaseTotals.Values) <= Objective.SumOfSquares(greedy.PhaseTotals.Values) + 1e-6);
        }
    }
}
