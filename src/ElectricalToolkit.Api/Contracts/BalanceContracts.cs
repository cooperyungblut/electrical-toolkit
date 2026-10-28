using System.ComponentModel.DataAnnotations;
using ElectricalToolkit.Core.Panels;

namespace ElectricalToolkit.Api.Contracts;

// What the client sends. These are the API's contract, kept separate from the Core types
// so the domain can change without breaking callers, and so Core has no web dependencies.
public sealed record BalanceRequest(
    PanelSystem System,
    int Spaces,
    [Required] IReadOnlyList<LoadRequest>? Loads);

public sealed record LoadRequest(
    [Required] string? Name,
    double Va,
    int Poles = 1);

// What the API sends back.
public sealed record BalanceResponse(
    IReadOnlyList<PlacementResponse> Placements,
    IReadOnlyDictionary<Phase, double> PhaseTotals,
    double ImbalancePercent);

public sealed record PlacementResponse(
    string Name,
    double Va,
    int Poles,
    IReadOnlyList<int> Slots,
    IReadOnlyList<Phase> Phases);