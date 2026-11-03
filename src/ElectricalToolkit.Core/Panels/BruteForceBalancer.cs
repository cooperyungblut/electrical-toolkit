namespace ElectricalToolkit.Core.Panels;

public sealed class BruteForceBalancer : IPanelBalancer
{
    public const int MaxLoads = 12; // So that this doesn't take 3500 years to finish

    public BalanceResult Balance(PanelConfiguration panel, IReadOnlyList<Load> loads)
    {
        LoadValidation.Validate(panel, loads);
        if (loads.Count() > MaxLoads) throw new ArgumentException("Too many loads for Brute Force");
        var ledger = new PhaseLedger(panel);
        var options = loads.Select(load => PhaseOptions.For(panel, load)).ToList();
        var current = new IReadOnlyList<Phase>[loads.Count];
        IReadOnlyList<Phase>[]? best = null;
        var bestScore = double.MaxValue;
        void Search(int i)
        {
            if (i == loads.Count)
            {
                double score = Objective.SumOfSquares(ledger.Totals.Values);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = current.ToArray();
                }
                return;
            }
            foreach (var option in options[i])
            {
                if (!ledger.Fits(option)) { continue; }
                ledger.Add(loads[i], option);
                current[i] = option;
                Search(i + 1);
                ledger.Remove(loads[i], option);
            }
        }
        Search(0); //Just 0 to start
        if (best is null) throw new ArgumentException("The loads don't fit on this panel's phases. Add spaces to the panel.");
        return ResultBuilder.Build(panel, loads.Zip(best));
    }
}
