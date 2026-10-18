using System.Net;
using System.Net.Http;
using System.Text.Json;
using Esgo.Models;
using Xunit;

namespace Esgo.Tests;

public class ResourceTests
{
    [Fact]
    public async Task Search_sends_snake_case_and_enum_values_as_in_the_spec()
    {
        var api = new FakeApi(_ => FakeApi.Json(HttpStatusCode.OK,
            "{\"object\":\"match_search\",\"distance_km\":612.4,\"match_count\":1,\"data\":[" + FakeApi.Match + "]}"));
        using var client = FakeApi.Client(api);

        var result = await client.Matches.SearchAsync(new MatchSearchRequest
        {
            Origin = "47057 Duisburg",
            Destination = "80331 München",
            WeightKg = 12000,
            LoadingRequirement = MatchSearchRequestLoadingRequirement.Seite,
        });

        var (request, body) = api.Seen.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v1/matches/search", request.RequestUri!.AbsolutePath);
        using var sent = JsonDocument.Parse(body!);
        Assert.Equal("47057 Duisburg", sent.RootElement.GetProperty("origin").GetString());
        Assert.Equal(12000, sent.RootElement.GetProperty("weight_kg").GetDouble());
        Assert.Equal("seite", sent.RootElement.GetProperty("loading_requirement").GetString());
        Assert.False(sent.RootElement.TryGetProperty("loading_meters", out _));   // null is not sent
        Assert.False(sent.RootElement.TryGetProperty("service_ids", out _));

        Assert.Equal(1, result.MatchCount);
        var match = result.Data[0];
        Assert.Equal("r_1", match.RouteId);
        Assert.Equal(new DateTime(2026, 10, 20, 8, 0, 0), match.DepartureWindow.Start.DateTime);
        Assert.Equal(640, match.PriceEstimateEur);
        Assert.Equal("https://es-go.de/booking_request/r_1", match.BookingUrl);
    }

    [Fact]
    public async Task Create_sends_time_windows_and_returns_open_request()
    {
        var api = new FakeApi(_ => FakeApi.Json(HttpStatusCode.Created, """
            {"object":"open_request","id":"or_1","status":"open","matches_found":2,
             "created_at":"2026-10-17T10:00:00","expires_at":"2026-10-21T18:00:00",
             "dashboard_url":"https://es-go.de/buyer/requests/or_1/matches"}
            """));
        using var client = FakeApi.Client(api);
        var start = new DateTimeOffset(2026, 11, 3, 8, 0, 0, TimeSpan.FromHours(1));

        var created = await client.OpenRequests.CreateAsync(new OpenRequestCreate
        {
            Origin = "47057 Duisburg",
            Destination = "80331 München",
            WeightKg = 12000,
            LoadingMeters = 4.8,
            CargoType = "Paletten",
            TimeWindows = new List<TimeWindow> { new() { Start = start, End = start.AddHours(8) } },
        });

        using var sent = JsonDocument.Parse(api.Seen.Single().Body!);
        var window = sent.RootElement.GetProperty("time_windows")[0];
        Assert.Equal(start, window.GetProperty("start").GetDateTimeOffset());
        Assert.Equal(start.AddHours(8), window.GetProperty("end").GetDateTimeOffset());
        Assert.Equal("Paletten", sent.RootElement.GetProperty("cargo_type").GetString());
        Assert.Equal("or_1", created.Id);
        Assert.Equal(2, created.MatchesFound);
    }

    [Fact]
    public async Task List_matches_escapes_the_path_parameter()
    {
        var api = new FakeApi(_ => FakeApi.Json(HttpStatusCode.OK, "{\"object\":\"list\",\"data\":[" + FakeApi.Match + "]}"));
        using var client = FakeApi.Client(api);

        var list = await client.OpenRequests.ListMatchesAsync("or/1");

        Assert.Equal("/v1/open-requests/or%2F1/matches", api.Seen.Single().Request.RequestUri!.AbsolutePath);
        Assert.Equal("r_1", list.Data[0].RouteId);
    }

    [Fact]
    public async Task Arguments_are_checked_before_any_request()
    {
        var api = new FakeApi(_ => throw new InvalidOperationException("must not be called"));
        using var client = FakeApi.Client(api);

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.Matches.SearchAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => client.OpenRequests.ListMatchesAsync(""));
        Assert.Empty(api.Seen);
    }
}
