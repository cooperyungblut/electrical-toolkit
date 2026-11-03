namespace ElectricalToolkit.Core.Panels;

// Stage 2: turn "which phases each load is on" into real slot numbers.
// A k-pole breaker starting at slot n takes n, n+2, n+4: the same side of the panel, in adjacent rows.
public static class SlotPacker
{
    // Backtracking gives up after this many tries, so a panel that can't be packed fails fast.
    public const int SearchLimit = 100_000;

    public static IReadOnlyList<Placement> Pack(
        PanelConfiguration panel,
        IReadOnlyList<(Load Load, IReadOnlyList<Phase> Phases)> assignments)
    {
        var taken = new bool[panel.Spaces + 1];
        var slotsOf = new IReadOnlyList<int>?[assignments.Count];

        // Pinned breakers keep their slots. Validation already checked they fit and don't overlap.
        for (int i = 0; i < assignments.Count; i++)
        {
            if (assignments[i].Load.FixedStartSlot is int start)
            {
                slotsOf[i] = SlotsFrom(start, assignments[i].Load.Poles);
                SetTaken(taken, slotsOf[i]!, true);
            }
        }

        // Multi-pole breakers need runs of open slots, so they go next, biggest first.
        // Each one takes the lowest start that works. If a later one can't fit, back up and
        // try the next start for an earlier one.
        var multi = Enumerable.Range(0, assignments.Count)
            .Where(i => assignments[i].Load.Poles > 1 && !assignments[i].Load.IsPinned)
            .OrderByDescending(i => assignments[i].Load.Poles)
            .ToList();
        int tries = 0;

        bool PlaceMulti(int k)
        {
            if (k == multi.Count)
            {
                return true;
            }
            if (++tries > SearchLimit)
            {
                return false;
            }

            int i = multi[k];
            var (load, phases) = assignments[i];
            for (int start = 1; start <= panel.Spaces; start++)
            {
                var slots = SlotsFrom(start, load.Poles);
                if (!IsOpen(panel, taken, slots, phases))
                {
                    continue;
                }

                SetTaken(taken, slots, true);
                slotsOf[i] = slots;
                if (PlaceMulti(k + 1))
                {
                    return true;
                }
                SetTaken(taken, slots, false);
                slotsOf[i] = null;
            }
            return false;
        }

        if (!PlaceMulti(0))
        {
            throw new ArgumentException("The multi-pole breakers don't fit in the open spaces. Add spaces or move pinned breakers.");
        }

        // 1-pole breakers take the lowest open slot on their phase. Stage 1 counted slots per phase,
        // so there is always one left.
        for (int i = 0; i < assignments.Count; i++)
        {
            if (slotsOf[i] is not null)
            {
                continue;
            }
            var phase = assignments[i].Phases[0];
            int slot = Enumerable.Range(1, panel.Spaces).FirstOrDefault(s => !taken[s] && panel.PhaseOfSlot(s) == phase);
            if (slot == 0)
            {
                throw new ArgumentException($"No open slot left on phase {phase}. Add spaces to the panel.");
            }
            taken[slot] = true;
            slotsOf[i] = [slot];
        }

        return assignments
            .Select((a, i) => new Placement(a.Load, slotsOf[i]!, slotsOf[i]!.Select(panel.PhaseOfSlot).ToList()))
            .ToList();
    }

    public static IReadOnlyList<int> SlotsFrom(int start, int poles) =>
        Enumerable.Range(0, poles).Select(k => start + 2 * k).ToList();

    private static bool IsOpen(PanelConfiguration panel, bool[] taken, IReadOnlyList<int> slots, IReadOnlyList<Phase> phases) =>
        slots.All(s => s <= panel.Spaces && !taken[s])
        && PhaseOptions.SameSet(slots.Select(panel.PhaseOfSlot).ToList(), phases);

    private static void SetTaken(bool[] taken, IReadOnlyList<int> slots, bool value)
    {
        foreach (var s in slots)
        {
            taken[s] = value;
        }
    }
}
