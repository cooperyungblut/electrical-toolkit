namespace ElectricalToolkit.Core.Panels;

public sealed class LocalSearchBalancer : IPanelBalancer
{
    private const double Epsilon = 1e-6;

    public BalanceResult Balance(PanelConfiguration panel, IReadOnlyList<Load> loads)
    {
        LoadValidation.Validate(panel, loads);
        var start = GreedyBalancer.Assign(panel, loads);
        List<Load> loadList = start.Select(a => a.Load).ToList();
        List<IReadOnlyList<Phase>> phasesOf = start.Select(a => a.Phases).ToList();
        var options = loadList.Select(load => PhaseOptions.For(panel, load)).ToList();
        var ledger = new PhaseLedger(panel);
        foreach (var (load, phases) in start)
        {
            ledger.Add(load, phases);
        }

        // Every accepted move lowers the objective by more than Epsilon, and there are finitely
        // many assignments, so this always stops.
        bool improved = true;
        while (improved)
        {
            improved = false;

            // Move one load: a 1-pole load to another phase, or a 2-pole load to another phase pair.
            for (int i = 0; i < loadList.Count; i++)
            {
                foreach (var option in options[i])
                {
                    var from = phasesOf[i];
                    if (PhaseOptions.SameSet(option, from) || !ledger.FitsMove(from, option))
                    {
                        continue;
                    }
                    if (ledger.Delta([(loadList[i], from, option)]) < -Epsilon)
                    {
                        ledger.Move(loadList[i], from, option);
                        phasesOf[i] = option;
                        improved = true;
                    }
                }
            }

            // Swap two loads with the same pole count. Each phase keeps the same number of
            // used slots, so capacity can't be broken.
            for (int i = 0; i < loadList.Count; i++)
            {
                for (int j = i + 1; j < loadList.Count; j++)
                {
                    if (loadList[i].Poles != loadList[j].Poles || PhaseOptions.SameSet(phasesOf[i], phasesOf[j]))
                    {
                        continue;
                    }
                    var (pi, pj) = (phasesOf[i], phasesOf[j]);
                    if (ledger.Delta([(loadList[i], pi, pj), (loadList[j], pj, pi)]) < -Epsilon)
                    {
                        ledger.Move(loadList[i], pi, pj);
                        ledger.Move(loadList[j], pj, pi);
                        (phasesOf[i], phasesOf[j]) = (pj, pi);
                        improved = true;
                    }
                }
            }
        }

        return ResultBuilder.Build(panel, loadList.Zip(phasesOf));
    }
}
