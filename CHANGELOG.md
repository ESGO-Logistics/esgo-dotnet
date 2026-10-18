# Changelog

All notable changes to this package are documented here. Versions follow
[Semantic Versioning](https://semver.org/). Before 1.0, minor versions may
contain breaking changes; they are marked as such.

## 0.1.0 (unreleased)

First release, all endpoints of API v1.

- `EsgoClient` with `Me.RetrieveAsync()`, `Matches.SearchAsync(...)`,
  `OpenRequests.CreateAsync(...)` and `OpenRequests.ListMatchesAsync(requestId)`.
- API key from the constructor, `EsgoConfiguration.ApiKey` or the
  `ESGO_API_KEY` environment variable.
- Typed models in `Esgo.Models`, generated from the OpenAPI spec with NSwag
  (`tools/Generate`).
- Typed exceptions for the ESGO error envelope (`RateLimitException` with
  `RetryAfterSeconds`, `NotFoundException`, ...).
- Targets `netstandard2.0` (.NET Framework 4.6.2+), `net8.0` and `net10.0`.
