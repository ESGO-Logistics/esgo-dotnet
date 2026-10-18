# ESGO .NET SDK

*[Deutsche Version weiter unten](#esgo-net-sdk-deutsch)*

Official .NET client for the [ESGO API](https://es-go.de/developers): find
matching empty-run capacity for road freight and publish open transport
requests from your TMS or ERP.

> **Alpha.** Covers all endpoints of API v1: key info, matching search and open
> requests. The SDK may still change before 1.0; see [CHANGELOG.md](CHANGELOG.md).

## Installation

```sh
dotnet add package Esgo
```

Targets .NET 8, .NET 10 and .NET Standard 2.0 (so .NET Framework 4.6.2 and newer).

## Access

API keys are issued by ESGO; there is no self-service sign-up yet. Request
access at [es-go.de/developers](https://es-go.de/developers). Keep your key
secret and never commit it to a repository.

## Usage

```csharp
using Esgo;
using Esgo.Models;

using var esgo = new EsgoClient("esgo_live_...");

var result = await esgo.Matches.SearchAsync(new MatchSearchRequest
{
    Origin = "47057 Duisburg",
    Destination = "80331 München",
    WeightKg = 12000,
    LoadingMeters = 4.8,
});

foreach (var match in result.Data)
    Console.WriteLine($"{match.RouteId} {match.DepartureWindow.Start} {match.PriceEstimateEur} {match.BookingUrl}");
```

Or set the key once, like `StripeConfiguration.ApiKey`:

```csharp
EsgoConfiguration.ApiKey = "esgo_live_...";   // or the ESGO_API_KEY environment variable

using var esgo = new EsgoClient();
var me = await esgo.Me.RetrieveAsync();
Console.WriteLine($"{me.AccountRole}: {string.Join(", ", me.Quotas.Select(q => $"{q.Name} {q.Remaining}"))}");
```

To use your own `HttpClient` (for example from `IHttpClientFactory`), pass it
as `new EsgoClient(apiKey, httpClient: client)`; the SDK does not dispose it.

### Open requests

Publish a transport request; ESGO matches it against carriers' empty runs:

```csharp
var start = new DateTimeOffset(2026, 11, 3, 8, 0, 0, TimeSpan.FromHours(1));

var request = await esgo.OpenRequests.CreateAsync(new OpenRequestCreate
{
    Origin = "47057 Duisburg",
    Destination = "80331 München",
    WeightKg = 12000,
    LoadingMeters = 4.8,
    CargoType = "Paletten",
    TimeWindows = new List<TimeWindow> { new() { Start = start, End = start.AddHours(8) } },
});

var matches = await esgo.OpenRequests.ListMatchesAsync(request.Id);
```

Times are local time Europe/Berlin.

| Method | Endpoint |
|---|---|
| `Me.RetrieveAsync()` | `GET /v1/me` |
| `Matches.SearchAsync(MatchSearchRequest)` | `POST /v1/matches/search` |
| `OpenRequests.CreateAsync(OpenRequestCreate)` | `POST /v1/open-requests` |
| `OpenRequests.ListMatchesAsync(requestId)` | `GET /v1/open-requests/{request_id}/matches` |

All fields are described in the [API reference](https://api.es-go.de/v1/docs).

## Errors

Every API error throws a subclass of `EsgoApiException` with the fields of the
ESGO error envelope:

```csharp
try
{
    await esgo.Me.RetrieveAsync();
}
catch (RateLimitException e)
{
    Console.WriteLine($"Try again in {e.RetryAfterSeconds} seconds");
}
catch (EsgoApiException e)
{
    Console.WriteLine($"{e.Status} {e.Code} {e.ErrorMessage} {e.Param} {e.RequestId}");
}
```

| Exception | HTTP status |
|---|---|
| `InvalidRequestException` | 400, 422 |
| `AuthenticationException` | 401 |
| `PermissionDeniedException` | 403 |
| `NotFoundException` | 404 |
| `RateLimitException` | 429 |
| `ServiceUnavailableException` | 503 |
| `InternalServerException` | other 5xx |
| `EsgoConnectionException` | network error or timeout, no response |

## Links

- API reference: [api.es-go.de/v1/docs](https://api.es-go.de/v1/docs)
- OpenAPI spec: [api.es-go.de/v1/openapi.json](https://api.es-go.de/v1/openapi.json)
- Python SDK: [pypi.org/project/esgo](https://pypi.org/project/esgo/)
- Changelog: [CHANGELOG.md](CHANGELOG.md)

## License

MIT, see [LICENSE](LICENSE).

---

# ESGO .NET SDK (Deutsch)

Offizieller .NET-Client für die [ESGO API](https://es-go.de/developers):
passende Leerfahrten im Straßengüterverkehr finden und offene
Transportanfragen direkt aus dem TMS oder ERP einstellen.

> **Alpha.** Deckt alle Endpunkte der API v1 ab: Schlüssel-Infos,
> Matching-Suche und offene Transportanfragen. Bis Version 1.0 kann sich das SDK
> noch ändern, siehe [CHANGELOG.md](CHANGELOG.md).

## Installation

```sh
dotnet add package Esgo
```

Für .NET 8, .NET 10 und .NET Standard 2.0 (damit auch .NET Framework 4.6.2 und neuer).

## Zugang

API-Schlüssel vergibt ESGO direkt; eine Registrierung per Self-Service gibt es
noch nicht. Den Zugang fragst du unter
[es-go.de/developers](https://es-go.de/developers) an. Halte deinen Schlüssel
geheim und committe ihn nie in ein Repository.

## Verwendung

Die Beispiele oben gelten unverändert: `new EsgoClient("esgo_live_...")` oder
einmalig `EsgoConfiguration.ApiKey` bzw. die Umgebungsvariable `ESGO_API_KEY`
setzen. Zeiten sind Ortszeit Europe/Berlin. Alle Felder stehen in der
[API-Referenz](https://api.es-go.de/v1/docs).

## Fehlerbehandlung

Jeder Fehler der API wirft eine Unterklasse von `EsgoApiException` mit den
Feldern der ESGO-Fehlerhülle (`Status`, `Code`, `ErrorMessage`, `Param`,
`RequestId`, bei 429 zusätzlich `RetryAfterSeconds`). Die Zuordnung zu den
HTTP-Status steht in der Tabelle oben.

## Lizenz

MIT, siehe [LICENSE](LICENSE).
