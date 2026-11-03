using ElectricalToolkit.Core.Panels;

namespace ElectricalToolkit.Core.Tests.Panels;

// Pinned loads (FixedStartSlot): breakers already in an existing panel.
public class PinnedLoadTests
{
    private static readonly PanelConfiguration Panel42 = new(PanelSystem.ThreePhase208Y120, 42);

    public static TheoryData<string> Balancers => ["Greedy", "LocalSearch", "BruteForce"];

    private static IPanelBalancer Make(string name) => name switch
    {
        "Greedy" => new GreedyBalancer(),
        "LocalSearch" => new LocalSearchBalancer(),
        "BruteForce" => new BruteForceBalancer(),
        _ => throw new ArgumentException(name),
    };

    [Theory]
    [MemberData(nameof(Balancers))]
    public void Pinned_loads_stay_in_their_slots_and_count_in_the_totals(string balancer)
    {
        // Slot 5 is phase C and slot 8 is phase A, so A and C start out loaded and the new load goes on B.
        List<Load> loads =
        [
            new("Existing 1", 3000, FixedStartSlot: 5),
            new("Existing 2", 3000, FixedStartSlot: 8),
            new("New", 3000),
        ];
        var result = Make(balancer).Balance(Panel42, loads);

        Assert.Equal([5], result.Placements.Single(p => p.Load.Name == "Existing 1").Slots);
        Assert.Equal([8], result.Placements.Single(p => p.Load.Name == "Existing 2").Slots);
        Assert.Equal(Phase.B, result.Placements.Single(p => p.Load.Name == "New").Phases[0]);
        Assert.All(result.PhaseTotals.Values, t => Assert.Equal(3000, t, precision: 6));
        MultipoleTests.AssertPhysicallyValid(Panel42, loads, result);
    }

    [Theory]
    [MemberData(nameof(Balancers))]
    public void Pinned_two_pole_takes_its_slot_and_the_one_two_below(string balancer)
    {
        List<Load> loads = [new("Existing dryer", 5000, 2, FixedStartSlot: 3), new("New", 1000), new("New 2", 1000, 2)];
        var result = Make(balancer).Balance(Panel42, loads);

        var dryer = result.Placements.Single(p => p.Load.Name == "Existing dryer");
        Assert.Equal([3, 5], dryer.Slots);
        Assert.Equal([Phase.B, Phase.C], dryer.Phases);
        MultipoleTests.AssertPhysicallyValid(Panel42, loads, result);
    }

    [Fact]
    public void Local_search_never_moves_a_pinned_load()
    {
        // Without the pin, local search would spread these out. Pinned, both stay on A.
        List<Load> loads = [new("P1", 5000, FixedStartSlot: 1), new("P2", 5000, FixedStartSlot: 2), new("L1", 100), new("L2", 100)];
        var result = new LocalSearchBalancer().Balance(Panel42, loads);
        Assert.Equal(10_000, result.PhaseTotals[Phase.A], precision: 6);
        Assert.Equal([1], result.Placements.Single(p => p.Load.Name == "P1").Slots);
        Assert.Equal([2], result.Placements.Single(p => p.Load.Name == "P2").Slots);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(43, 1)]
    [InlineData(41, 2)]   // 41 and 43, and there is no 43
    [InlineData(39, 3)]   // 39, 41, 43
    public void Pin_outside_the_panel_is_rejected(int start, int poles) =>
        Assert.Throws<ArgumentException>(() =>
            new GreedyBalancer().Balance(Panel42, [new Load("Existing", 1000, poles, FixedStartSlot: start)]));

    [Fact]
    public void Overlapping_pins_are_rejected() =>
        // A 2-pole at 1 covers 1 and 3.
        Assert.Throws<ArgumentException>(() => new GreedyBalancer().Balance(Panel42,
            [new Load("Existing 1", 1000, 2, FixedStartSlot: 1), new Load("Existing 2", 1000, FixedStartSlot: 3)]));

    [Fact]
    public void Tight_panel_that_cannot_be_packed_gives_a_clear_error()
    {
        // 12 spaces. Pins at 5 and 11 (left side) and 2 and 8 (right side) leave no run of three
        // open rows on either side, though every phase still has an open slot for a 3-pole breaker.
        var panel12 = new PanelConfiguration(PanelSystem.ThreePhase208Y120, 12);
        List<Load> loads =
        [
            new("E1", 500, FixedStartSlot: 5),
            new("E2", 500, FixedStartSlot: 11),
            new("E3", 500, FixedStartSlot: 2),
            new("E4", 500, FixedStartSlot: 8),
            new("Motor", 3000, 3),
        ];
        var ex = Assert.Throws<ArgumentException>(() => new LocalSearchBalancer().Balance(panel12, loads));
        Assert.Contains("don't fit", ex.Message);
    }
}
