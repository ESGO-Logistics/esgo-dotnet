using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Xunit;

namespace Esgo.Tests;

public class ErrorTests
{
    public static TheoryData<int, string, string, Type> Cases => new()
    {
        { 401, "authentication_error", "invalid_api_key", typeof(AuthenticationException) },
        { 403, "permission_error", "insufficient_scope", typeof(PermissionDeniedException) },
        { 404, "invalid_request_error", "resource_missing", typeof(NotFoundException) },
        { 422, "invalid_request_error", "invalid_parameter", typeof(InvalidRequestException) },
        { 503, "api_error", "api_disabled", typeof(ServiceUnavailableException) },
        { 500, "api_error", "internal_error", typeof(InternalServerException) },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Error_envelope_becomes_typed_exception(int status, string type, string code, Type expected)
    {
        var api = new FakeApi(_ => FakeApi.Error(status, type, code, "Something", "time_windows.0.end"));
        using var client = FakeApi.Client(api);

        var ex = await Assert.ThrowsAnyAsync<EsgoApiException>(() => client.Me.RetrieveAsync());

        Assert.IsType(expected, ex);
        Assert.Equal(status, ex.Status);
        Assert.Equal(type, ex.Type);
        Assert.Equal(code, ex.Code);
        Assert.Equal("time_windows.0.end", ex.Param);
        Assert.Equal("Something", ex.ErrorMessage);
        Assert.Equal("req_123", ex.RequestId);
    }

    [Fact]
    public async Task Rate_limit_carries_retry_after()
    {
        var api = new FakeApi(_ =>
        {
            var response = FakeApi.Error(429, "rate_limit_error", "daily_quota_exceeded", "Quota");
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(3600));
            return response;
        });
        using var client = FakeApi.Client(api);

        var ex = await Assert.ThrowsAsync<RateLimitException>(() => client.Me.RetrieveAsync());
        Assert.Equal(3600, ex.RetryAfterSeconds);
        Assert.Equal("daily_quota_exceeded", ex.Code);
    }

    [Fact]
    public async Task Non_json_error_still_raises_with_status()
    {
        var api = new FakeApi(_ => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("Bad Gateway") });
        using var client = FakeApi.Client(api);

        var ex = await Assert.ThrowsAsync<InternalServerException>(() => client.Me.RetrieveAsync());
        Assert.Equal(502, ex.Status);
        Assert.Null(ex.Code);
        Assert.Equal("Bad Gateway", ex.Body);
    }

    [Fact]
    public async Task Network_error_becomes_connection_exception()
    {
        var api = new FakeApi(_ => throw new HttpRequestException("refused"));
        using var client = FakeApi.Client(api);

        await Assert.ThrowsAsync<EsgoConnectionException>(() => client.Me.RetrieveAsync());
    }
}
