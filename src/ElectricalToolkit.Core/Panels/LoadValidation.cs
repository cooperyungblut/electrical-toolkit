namespace ElectricalToolkit.Core.Panels;

public static class LoadValidation
{
    public static void Validate(PanelConfiguration panel, IReadOnlyList<Load> loads)
    {
        if (loads.Count > panel.Spaces) throw new ArgumentException($"Too many loads", nameof(loads));
        
        foreach (var load in loads)
        {
            if (string.IsNullOrWhiteSpace(load.Name)) throw new ArgumentException("Load missing a Name");
            if (load.Poles != 1) throw new NotSupportedException($"{load.Name}: Only 1-pole loads are supported in this version");
            if (load.Va <= 0) throw new ArgumentException($"{load.Name}: VA is less than or equal to 0");
        }
    }
}