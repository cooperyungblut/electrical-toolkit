namespace ElectricalToolkit.Core.Panels;

public sealed class LocalSearchBalancer : IPanelBalancer
{
    public BalanceResult Balance(PanelConfiguration panel, IReadOnlyList<Load> loads)
    {
        var start = new GreedyBalancer().Balance(panel, loads); // also validates
        var totals = panel.Phases.ToDictionary(p => p, p => start.PhaseTotals[p]);
        var capacity = panel.Phases.ToDictionary(p => p, p => panel.SlotsOnPhase(p).Count());
        List<Load> loadList = start.Placements.Select(placement => placement.Load).ToList();
        List<Phase> phaseOf = start.Placements.Select(placement => placement.Phases[0]).ToList();
        var used = panel.Phases.ToDictionary(p => p, p => phaseOf.Count(ph => ph == p));
        bool Helps(double x, Phase from, Phase to)
        {
            return 1e-6 < x && 1e-6 < (totals[from] - totals[to] - x);
        }

        bool improved = true;
        while (improved)
        {
            improved = false;
            for (int i = 0; i < loadList.Count; i++)
            {
                foreach (var p in panel.Phases)
                {
                    if (p == phaseOf[i]) continue;
                    var from = phaseOf[i];
                    if (used[p] < capacity[p] && Helps(loadList[i].Va, from, p)) // Move One at a time
                    {
                        phaseOf[i] = p;
                        totals[p] += loadList[i].Va;
                        totals[from] -= loadList[i].Va;
                        used[p]++;
                        used[from]--;
                        improved = true;
                    }
                }
            }

            for (int i = 0; i < loadList.Count; i++)
            {
                for (int j = i + 1; j < loadList.Count; j++)
                {
                    if (phaseOf[i] == phaseOf[j]) continue;
                    var (big, small) = loadList[i].Va >= loadList[j].Va ? (i, j) : (j, i);
                    var bigPhase = phaseOf[big];
                    var smallPhase = phaseOf[small];
                    var d = loadList[big].Va - loadList[small].Va;
                    if (Helps(d, bigPhase, smallPhase)) // Swap two loads
                    {
                        (phaseOf[i], phaseOf[j]) = (phaseOf[j], phaseOf[i]);
                        totals[bigPhase] -= d;
                        totals[smallPhase] += d;
                        improved = true;
                    }
                }
            }
        }

        return ResultBuilder.Build(panel, loadList.Select((load, i) => (load, phaseOf[i])));
    }
}