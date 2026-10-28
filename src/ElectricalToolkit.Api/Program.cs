using System.Text.Json.Serialization;
using ElectricalToolkit.Api.Endpoints;
using ElectricalToolkit.Core.Panels;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Electrical Design Toolkit API";
        document.Info.Description =
            "Calculation aid only. Results do not replace the judgment of a qualified engineer " +
            "and must be verified against the governing code, manufacturer data and project requirements.";
        return Task.CompletedTask;
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IPanelBalancer, LocalSearchBalancer>();

var app = builder.Build();

// Malformed JSON (a typo'd enum name, a string where a number goes) is the client's fault: 400, not 500.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = ex => ex is BadHttpRequestException bad ? bad.StatusCode : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
app.MapBalanceEndpoints();

app.Run();