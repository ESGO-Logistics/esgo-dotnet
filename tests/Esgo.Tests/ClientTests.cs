using System.Net;
using System.Net.Http;
using Xunit;

namespace Esgo.Tests;

[Collection("EsgoConfiguration")]   // tests touching static configuration must not run in parallel
public class ClientTests
{
    [Fact]
    public async Task Sends_key_user_agent_and_reads_typed_response()
    {
        var api = new FakeApi(_ => FakeApi.Json(HttpStatusCode.OK, FakeApi.Me));
        using var client = FakeApi.Client(api);

        var me = await client.Me.RetrieveAsync();

        var request = api.Seen.Single().Request;
        Assert.Equal("https://api.es-go.de/v1/me", request.RequestUri!.ToString());
        Assert.Equal("Bearer esgo_live_test", request.Headers.Authorization!.ToString());
        Assert.StartsWith("esgo-dotnet/", request.Headers.UserAgent.ToString());
        Assert.Equal("buyer", me.AccountRole);
        Assert.Equal(199, me.Quotas[0].Remaining);
        Assert.Equal(new DateTimeOffset(2026, 10, 17, 10, 0, 0, TimeSpan.Zero), me.Key.CreatedAt);
        Assert.Null(me.Key.ExpiresAt);
    }

    [Fact]
    public async Task Global_configuration_like_stripe()
    {
        var api = new FakeApi(_ => FakeApi.Json(HttpStatusCode.OK, FakeApi.Me));
        EsgoConfiguration.ApiKey = "esgo_live_global";
        try
        {
            using var client = new EsgoClient(httpClient: new HttpClient(api));
            await client.Me.RetrieveAsync();
            Assert.Equal("Bearer esgo_live_global", api.Seen.Single().Request.Headers.Authorization!.ToString());
        }
        finally
        {
            EsgoConfiguration.ApiKey = null;
        }
    }

    [Fact]
    public void Key_from_environment()
    {
        Environment.SetEnvironmentVariable("ESGO_API_KEY", "esgo_live_env");
        try
        {
            using var client = new EsgoClient();
            Assert.Equal("esgo_live_env", client.ApiKey);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ESGO_API_KEY", null);
        }
    }

    [Fact]
    public void Missing_key_is_a_clear_error()
    {
        Environment.SetEnvironmentVariable("ESGO_API_KEY", null);
        var ex = Assert.Throws<EsgoException>(() => new EsgoClient());
        Assert.Contains("No API key", ex.Message);
    }

    [Fact]
    public async Task Custom_base_url_is_used()
    {
        var api = new FakeApi(_ => FakeApi.Json(HttpStatusCode.OK, FakeApi.Me));
        using var client = new EsgoClient("esgo_live_test", "https://sandbox.example/", new HttpClient(api));
        await client.Me.RetrieveAsync();
        Assert.Equal("https://sandbox.example/v1/me", api.Seen.Single().Request.RequestUri!.ToString());
    }
}
