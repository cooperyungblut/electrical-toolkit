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

        var pinnedSlots = new HashSet<int>();
        foreach (var load in loads.Where(l => l.IsPinned))
        {
            var slots = SlotPacker.SlotsFrom(load.FixedStartSlot!.Value, load.Poles);
            if (slots[0] < 1 || slots[^1] > panel.Spaces)
            {
                throw new ArgumentException($"{load.Name}: a {load.Poles}-pole breaker can't start at slot {slots[0]} on a {panel.Spaces}-space panel");
            }
            if (!slots.All(pinnedSlots.Add))
            {
                throw new ArgumentException($"{load.Name}: its pinned slots overlap another pinned load");
            }
        }

        int needed = loads.Sum(l => l.Poles);
        if (needed > panel.Spaces)
        {
            throw new ArgumentException($"Too many loads: they need {needed} spaces and the panel has {panel.Spaces}", nameof(loads));
        }
    }
}
