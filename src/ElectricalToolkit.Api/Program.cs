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

// Serve the Blazor UI's files (the ElectricalToolkit.Web project) from this same app.
app.MapStaticAssets();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapBalanceEndpoints();

// Any other URL that isn't a file is a page in the UI, so hand it index.html and let Blazor's router decide.
// Unknown /api URLs still get a 404, so API clients never receive the HTML page by mistake.
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();