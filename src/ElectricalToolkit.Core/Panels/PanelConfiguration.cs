using System.Reflection.Metadata.Ecma335;

namespace ElectricalToolkit.Core.Panels;

public sealed record PanelConfiguration
{
    public PanelSystem System { get; }
    public int Spaces { get; }

    public PanelConfiguration(PanelSystem system, int spaces)
    {
        if ( spaces <= 0 || spaces % 2 != 0 )
            throw new ArgumentOutOfRangeException(nameof(spaces), "Spaces must be a positive even number.");
        System = system;
        Spaces = spaces;
    }

    // [A, B, C] for three-phase, [A, B] for single-phase
    public IReadOnlyList<Phase> Phases => System switch
    {
        PanelSystem.ThreePhase208Y120 => [Phase.A, Phase.B, Phase.C],
        PanelSystem.SinglePhase120240 => [Phase.A, Phase.B],
        _ => throw new InvalidOperationException($"Unknown panel system {System}"),
    };

    // Throw ArgumentOutOfRangeException if slot is outside 1..Spaces.
    public Phase PhaseOfSlot(int slot)
    {
        if (( slot > Spaces ) || ( slot < 1 )) throw new ArgumentOutOfRangeException(nameof(slot), slot, $"Slot must be between 1 and {Spaces}.");
        int row = (slot - 1) / 2;
        return Phases[row % Phases.Count];
    }

    // Every slot number on the given phase, ascending. Hint: LINQ, using PhaseOfSlot.
    public IEnumerable<int> SlotsOnPhase(Phase phase)
    {
        return Enumerable.Range(1, Spaces).Where(slot => PhaseOfSlot(slot) == phase);
    }
}