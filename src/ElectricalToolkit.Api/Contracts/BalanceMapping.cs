using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using ElectricalToolkit.Core.Panels;

namespace ElectricalToolkit.Api.Contracts;

public static class BalanceMapping
{
    public static PanelConfiguration ToPanel(this BalanceRequest request) => new PanelConfiguration(request.System, request.Spaces);

    public static List<Load> ToLoads(this BalanceRequest request) => 
        request.Loads!
            .Select((lr, i) => new Load (string.IsNullOrWhiteSpace(lr.Name) ? $"UL{i + 1}" : lr.Name,
        lr.Va, lr.Poles))
            .ToList();

    public static BalanceResponse ToResponse(this BalanceResult result) =>
        new BalanceResponse(result.Placements.Select( pment => new PlacementResponse(pment.Load.Name, pment.Load.Va, pment.Load.Poles,
        pment.Slots, pment.Phases)).ToList(),
        result.PhaseTotals,
        result.ImbalancePercent);
    
}