using ElectricalToolkit.Api.Contracts;
using ElectricalToolkit.Core.Panels;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ElectricalToolkit.Api.Endpoints;

public static class BalanceEndpoints
{
    public static IEndpointRouteBuilder MapBalanceEndpoints(this IEndpointRouteBuilder app)
    {
        var panels = app.MapGroup("/api/panels");

        panels.MapPost("/balance", Balance)
            .WithName("BalancePanel")
            .WithSummary("Assign loads to panel slots to minimize phase imbalance.");

        return app;
    }

    private static Results<Ok<BalanceResponse>, ProblemHttpResult> Balance(BalanceRequest request, IPanelBalancer balancer)
    {
        try
        {
            var panel = request.ToPanel();
            var loads = request.ToLoads();
            var result = balancer.Balance(panel, loads);
            return TypedResults.Ok(result.ToResponse());
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return TypedResults.Problem(
                title: "The panel or its loads are invalid.",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}