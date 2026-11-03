namespace ElectricalToolkit.Core.Panels;

// The sets of phases a load could sit on, before slot numbers are chosen.
// A breaker covers vertically adjacent rows, and the rows cycle A, B, C, so on a
// three-phase panel a 2-pole breaker is AB, BC or CA and a 3-pole breaker is always ABC.
public static class PhaseOptions
{
    public static IReadOnlyList<IReadOnlyList<Phase>> For(PanelConfiguration panel, Load load)
    {
        var phases = panel.Phases;
        if (load.Poles == phases.Count)
        {
            return [phases];
        }

        return Enumerable.Range(0, phases.Count)
            .Select(start => (IReadOnlyList<Phase>)Enumerable.Range(0, load.Poles)
                .Select(k => phases[(start + k) % phases.Count])
                .ToList())
            .ToList();
    }

    public static bool SameSet(IReadOnlyList<Phase> a, IReadOnlyList<Phase> b) =>
        a.Count == b.Count && a.All(b.Contains);
}
