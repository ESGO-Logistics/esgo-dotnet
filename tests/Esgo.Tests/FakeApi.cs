using System.Net;
using System.Net.Http;
using System.Text;

namespace Esgo.Tests;

/// <summary>Answers like the ESGO API and records every request.</summary>
internal sealed class FakeApi : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _answer;

    public FakeApi(Func<HttpRequestMessage, HttpResponseMessage> answer) => _answer = answer;

    public List<(HttpRequestMessage Request, string? Body)> Seen { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync();
        Seen.Add((request, body));
        return _answer(request);
    }

    public static HttpResponseMessage Json(HttpStatusCode status, string json)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Error(int status, string type, string code, string message, string? param = null)
    {
        var p = param is null ? "null" : "\"" + param + "\"";
        var response = Json((HttpStatusCode)status,
            $"{{\"error\":{{\"type\":\"{type}\",\"code\":\"{code}\",\"message\":\"{message}\",\"param\":{p}}}}}");
        response.Headers.Add("X-Request-Id", "req_123");
        return response;
    }

    public static EsgoClient Client(FakeApi api, string apiKey = "esgo_live_test")
        => new(apiKey, httpClient: new HttpClient(api));

    public const string Match = """
        {"object":"match","route_id":"r_1","origin_area":"Duisburg","destination_area":"München",
         "departure_window":{"start":"2026-10-20T08:00:00","end":"2026-10-20T12:00:00"},
         "free_capacity_kg":8000,"free_capacity_ldm":6.2,"instant_bookable":false,"match_score":87.5,
         "booking_url":"https://es-go.de/booking_request/r_1","detour_km":12,"price_estimate_eur":640}
        """;

    public const string Me = """
        {"object":"me","account_role":"buyer","quotas":[{"name":"matches_search","limit":200,"remaining":199}],
         "key":{"id":"k1","name":"TMS","prefix":"esgo_live_ab","scopes":["matches:read"],
                "created_at":"2026-10-17T10:00:00Z","expires_at":null}}
        """;
}
