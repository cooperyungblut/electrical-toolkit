using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ElectricalToolkit.Core.Panels;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ElectricalToolkit.Api.Tests;

// These run the real API in memory: routing, JSON, validation and the balancer,
// with no web server or network. Each test sends JSON the way a browser would.
public class BalanceEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BalanceEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private static object Request(int spaces, params object[] loads) =>
        new { system = "ThreePhase208Y120", spaces, loads };

    private static object Load(string? name, double va, int poles = 1, int? fixedStartSlot = null) =>
        new { name, va, poles, fixedStartSlot };

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task Valid_request_returns_balanced_totals()
    {
        var body = Request(42,
            Load("L1", 6000), Load("L2", 3000), Load("L3", 3000),
            Load("L4", 2000), Load("L5", 2000), Load("L6", 2000));

        var response = await _client.PostAsJsonAsync("/api/panels/balance", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await Json(response);
        Assert.Equal(6000, json.GetProperty("phaseTotals").GetProperty("A").GetDouble());
        Assert.Equal(6000, json.GetProperty("phaseTotals").GetProperty("B").GetDouble());
        Assert.Equal(6000, json.GetProperty("phaseTotals").GetProperty("C").GetDouble());
        Assert.Equal(0, json.GetProperty("imbalancePercent").GetDouble(), precision: 6);
        Assert.Equal(6, json.GetProperty("placements").GetArrayLength());
        Assert.Equal(50, json.GetProperty("phaseAmps").GetProperty("A").GetDouble(), precision: 6);
    }

    [Fact]
    public async Task Placements_come_back_with_slots_and_phase_names()
    {
        var response = await _client.PostAsJsonAsync("/api/panels/balance", Request(42, Load("Receptacles - Office 101", 1800)));

        var placement = (await Json(response)).GetProperty("placements")[0];
        Assert.Equal("Receptacles - Office 101", placement.GetProperty("name").GetString());
        Assert.Equal(1, placement.GetProperty("slots")[0].GetInt32());
        Assert.Equal("A", placement.GetProperty("phases")[0].GetString());
    }

    [Fact]
    public async Task Empty_load_list_returns_zeros()
    {
        var response = await _client.PostAsJsonAsync("/api/panels/balance", Request(42));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await Json(response);
        Assert.Equal(0, json.GetProperty("imbalancePercent").GetDouble());
        Assert.Equal(0, json.GetProperty("placements").GetArrayLength());
    }

    [Fact]
    public async Task Two_pole_load_comes_back_on_two_slots()
    {
        var response = await _client.PostAsJsonAsync("/api/panels/balance", Request(42, Load("Water heater", 4500, poles: 2)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var placement = (await Json(response)).GetProperty("placements")[0];
        Assert.Equal(2, placement.GetProperty("poles").GetInt32());
        Assert.Equal(1, placement.GetProperty("slots")[0].GetInt32());
        Assert.Equal(3, placement.GetProperty("slots")[1].GetInt32());
    }

    [Fact]
    public async Task Four_pole_load_returns_400_problem_details()
    {
        var response = await _client.PostAsJsonAsync("/api/panels/balance", Request(42, Load("Pump", 4500, poles: 4)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Poles", (await Json(response)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Pinned_load_stays_put_and_is_marked_pinned()
    {
        var body = Request(42, Load("Existing", 1000, fixedStartSlot: 7), Load("New", 1000));

        var response = await _client.PostAsJsonAsync("/api/panels/balance", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var existing = (await Json(response)).GetProperty("placements").EnumerateArray()
            .Single(p => p.GetProperty("name").GetString() == "Existing");
        Assert.Equal(7, existing.GetProperty("slots")[0].GetInt32());
        Assert.True(existing.GetProperty("pinned").GetBoolean());
    }

    [Fact]
    public async Task Overlapping_pins_return_400()
    {
        var body = Request(42, Load("E1", 1000, fixedStartSlot: 7), Load("E2", 1000, fixedStartSlot: 7));

        var response = await _client.PostAsJsonAsync("/api/panels/balance", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("ThreePhase480Y277", 3)]
    [InlineData("SinglePhase120240", 2)]
    public async Task Other_panel_systems_are_accepted(string system, int phaseCount)
    {
        var body = new { system, spaces = 24, loads = new[] { Load("Pump", 9000, poles: 2), Load("Lights", 1000) } };

        var response = await _client.PostAsJsonAsync("/api/panels/balance", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(phaseCount, (await Json(response)).GetProperty("phaseTotals").EnumerateObject().Count());
    }

    [Fact]
    public async Task Missing_load_list_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/panels/balance", new { system = "ThreePhase208Y120", spaces = 42 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Unknown_panel_system_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/panels/balance", new { system = "Delta480", spaces = 42, loads = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OpenApi_document_is_served()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/panels/balance", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Too_many_loads_returns_400()
    {
        var body = Request(4,
            Load("L1", 6000), Load("L2", 3000), Load("L3", 3000),
            Load("L4", 2000), Load("L5", 2000), Load("L6", 2000));
        
        var response = await _client.PostAsJsonAsync("/api/panels/balance", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Odd_spaces_returns_400()
    {
        var body = Request(41);
        
        var response = await _client.PostAsJsonAsync("/api/panels/balance", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("", 1200)]
	[InlineData("   ", 1000)]
    [InlineData(null, 1100)]
    public async Task Blank_name_becomes_UL_plus_row_number(string? name, double loadVa)
    {
        var testLoad = Load(name, loadVa);
        var body = Request(42, Load("L1", 6000), testLoad);
        
        var response = await _client.PostAsJsonAsync("/api/panels/balance", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var names = (await Json(response)).GetProperty("placements").EnumerateArray()
            .Select(p => p.GetProperty("name").GetString());
        Assert.Contains("UL2", names);
    }

    [Fact]
    public async Task Root_serves_the_ui()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }
}