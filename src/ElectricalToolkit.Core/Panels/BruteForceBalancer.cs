namespace ElectricalToolkit.Core.Panels;

public sealed class BruteForceBalancer : IPanelBalancer
{
    public const int MaxLoads = 12; // So that this doesn't take 3500 years to finish

    public BalanceResult Balance(PanelConfiguration panel, IReadOnlyList<Load> loads)
    {
        LoadValidation.Validate(panel, loads);
        if (loads.Count() > MaxLoads) throw new ArgumentException("Too many loads for Brute Force");
        var totals = panel.Phases.ToDictionary(p => p, _ => 0.0);
        var capacity = panel.Phases.ToDictionary(p => p, p => panel.SlotsOnPhase(p).Count());
        var used = panel.Phases.ToDictionary(p => p, _ => 0);
        var current = new Phase[loads.Count];
        Phase[] best = [];
        var bestScore = double.MaxValue;
        void Search(int i)
        {
            if (i == loads.Count)
            { 
                double score = Objective.SumOfSquares(totals.Values);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = current.ToArray();
                }
                return; 
            }
            foreach (var p in panel.Phases)
            {
                if (used[p] >= capacity[p]) { continue; }
                used[p]++;
                current[i] = p;
                totals[p] += loads[i].Va;
                Search(i + 1);
                used[p]--;
                totals[p] -= loads[i].Va;
            }
        }
        Search(0); //Just 0 to start
        return ResultBuilder.Build(panel, loads.Select((load, i) => (load, best[i])));
    }
}