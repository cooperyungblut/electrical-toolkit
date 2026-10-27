using ElectricalToolkit.Core.Panels;
using Xunit.Abstractions;

namespace ElectricalToolkit.Core.Tests.Panels;

// Runs all three balancers on the same seeded random panels and measures how close
// local search gets to the true optimum. The printed summary is the number for the README.
public class BalancerComparisonTests
{
    private readonly ITestOutputHelper _output;

    public BalancerComparisonTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static double Score(BalanceResult result) => Objective.SumOfSquares(result.PhaseTotals.Values);

    [Fact]
    public void Local_search_beats_or_ties_greedy_and_is_measured_against_brute_force()
    {
        var panel = new PanelConfiguration(PanelSystem.ThreePhase208Y120, 42);
        var rng = new Random(42); // fixed seed: the same "random" cases on every run
        const int cases = 500;
        int greedyOptimal = 0, localOptimal = 0;
        double worstLocalGap = 0; // percentage points of imbalance above the optimum

        for (int c = 0; c < cases; c++)
        {
            int n = rng.Next(1, 10);
            var loads = Enumerable.Range(1, n).Select(i => new Load($"L{i}", rng.Next(1, 50) * 100)).ToList();

            var greedy = new GreedyBalancer().Balance(panel, loads);
            var local = new LocalSearchBalancer().Balance(panel, loads);
            var best = new BruteForceBalancer().Balance(panel, loads);

            Assert.True(Score(local) <= Score(greedy) + 1e-6, $"Case {c}: local search made greedy worse.");
            Assert.True(Score(best) <= Score(local) + 1e-6, $"Case {c}: brute force wasn't optimal.");

            if (Score(greedy) - Score(best) < 1e-6) greedyOptimal++;
            if (Score(local) - Score(best) < 1e-6) localOptimal++;
            worstLocalGap = Math.Max(worstLocalGap, local.ImbalancePercent - best.ImbalancePercent);
        }

        _output.WriteLine($"{cases} random cases, 1-9 loads each:");
        _output.WriteLine($"  greedy optimal:       {greedyOptimal} ({100.0 * greedyOptimal / cases:F1}%)");
        _output.WriteLine($"  local search optimal: {localOptimal} ({100.0 * localOptimal / cases:F1}%)");
        _output.WriteLine($"  worst local search gap: {worstLocalGap:F1} imbalance points above optimal");
    }

    [Fact]
    public void Local_search_never_makes_a_full_panel_worse()
    {
        var panel = new PanelConfiguration(PanelSystem.ThreePhase208Y120, 42);
        var rng = new Random(7);
        for (int c = 0; c < 200; c++)
        {
            var loads = Enumerable.Range(1, rng.Next(10, 43)).Select(i => new Load($"L{i}", rng.Next(1, 100) * 50)).ToList();
            var greedy = new GreedyBalancer().Balance(panel, loads);
            var local = new LocalSearchBalancer().Balance(panel, loads);

            Assert.True(Score(local) <= Score(greedy) + 1e-6);
            Assert.Equal(loads.Sum(l => l.Va), local.PhaseTotals.Values.Sum(), precision: 6);
            Assert.Equal(loads.Count, local.Placements.SelectMany(p => p.Slots).Distinct().Count());
        }
    }
}