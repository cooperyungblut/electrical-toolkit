namespace ElectricalToolkit.Core.Panels;

public static class LoadValidation
{
    public static void Validate(PanelConfiguration panel, IReadOnlyList<Load> loads)
    {
        foreach (var load in loads)
        {
            if (string.IsNullOrWhiteSpace(load.Name)) throw new ArgumentException("Load missing a Name");
            if (load.Va <= 0) throw new ArgumentException($"{load.Name}: VA is less than or equal to 0");
            if (load.Poles < 1 || load.Poles > 3) throw new ArgumentException($"{load.Name}: Poles must be 1, 2 or 3");
            if (load.Poles > panel.Phases.Count)
            {
                throw new ArgumentException($"{load.Name}: A {load.Poles}-pole load needs a panel with at least {load.Poles} phases");
            }
        }

        int needed = loads.Sum(l => l.Poles);
        if (needed > panel.Spaces)
        {
            throw new ArgumentException($"Too many loads: they need {needed} spaces and the panel has {panel.Spaces}", nameof(loads));
        }
    }
}
