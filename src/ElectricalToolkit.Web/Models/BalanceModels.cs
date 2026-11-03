namespace ElectricalToolkit.Web.Models;

// The UI's copy of the API's JSON contract. It deliberately doesn't reference the Api or Core
// projects: the HTTP API is the only thing the UI depends on, the same as a React client would.
public sealed record BalanceRequest(string System, int Spaces, IReadOnlyList<LoadRequest> Loads);

public sealed record LoadRequest(string? Name, double Va, int Poles, int? FixedStartSlot);

public sealed record BalanceResponse(
    IReadOnlyList<PlacementResponse> Placements,
    IReadOnlyDictionary<string, double> PhaseTotals,
    double ImbalancePercent,
    IReadOnlyDictionary<string, double> PhaseAmps);

public sealed record PlacementResponse(
    string Name,
    double Va,
    int Poles,
    IReadOnlyList<int> Slots,
    IReadOnlyList<string> Phases,
    bool Pinned);

// The parts of a 400 ProblemDetails body the UI shows.
public sealed record Problem(string? Title, string? Detail);

// One editable row in the load table. A class, not a record, because @bind writes to its properties.
public sealed class LoadRow
{
    public string Name { get; set; } = "";
    public double Va { get; set; }
    public int Poles { get; set; } = 1;
    public int? PinSlot { get; set; }   // blank = let the balancer choose
}