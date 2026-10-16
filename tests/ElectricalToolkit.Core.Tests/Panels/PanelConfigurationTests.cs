using ElectricalToolkit.Core.Panels;

namespace ElectricalToolkit.Core.Tests.Panels;

public class PanelConfigurationTests
{
    [Theory]
    [InlineData(1, Phase.A)]
    [InlineData(2, Phase.A)]
    [InlineData(3, Phase.B)]
    [InlineData(4, Phase.B)]
    [InlineData(5, Phase.C)]
    [InlineData(6, Phase.C)]
    [InlineData(7, Phase.A)]
    [InlineData(42, Phase.C)]
    public void ThreePhase_slot_maps_to_expected_phase(int slot, Phase expected)
    {
        var panel = new PanelConfiguration(PanelSystem.ThreePhase208Y120, 42);
        Assert.Equal(expected, panel.PhaseOfSlot(slot));
    }

    [Theory]
    [InlineData(1, Phase.A)]
    [InlineData(3, Phase.B)]
    [InlineData(5, Phase.A)]
    [InlineData(6, Phase.A)]
    public void SinglePhase_slot_maps_to_expected_phase(int slot, Phase expected)
    {
        var panel = new PanelConfiguration(PanelSystem.SinglePhase120240, 24);
        Assert.Equal(expected, panel.PhaseOfSlot(slot));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(43)]
    public void Slot_outside_panel_throws(int slot)
    {
        var panel = new PanelConfiguration(PanelSystem.ThreePhase208Y120, 42);
        Assert.Throws<ArgumentOutOfRangeException>(() => panel.PhaseOfSlot(slot));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(41)]
    public void Invalid_space_count_throws(int spaces) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new PanelConfiguration(PanelSystem.ThreePhase208Y120, spaces));

    [Fact]
    public void Each_phase_gets_a_third_of_a_42_space_panel()
    {
        var panel = new PanelConfiguration(PanelSystem.ThreePhase208Y120, 42);
        Assert.All(panel.Phases, p => Assert.Equal(14, panel.SlotsOnPhase(p).Count()));
    }
}