using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Esgo.Resources;

namespace Esgo
{
    /// <summary>
    /// Client for the ESGO API.
    /// <code>
    /// using var esgo = new EsgoClient("esgo_live_...");
    /// var result = await esgo.Matches.SearchAsync(new MatchSearchRequest { Origin = "...", Destination = "...", WeightKg = 12000 });
    /// </code>
    /// </summary>
    public sealed class EsgoClient : IDisposable
    {
        /// <summary>Base URL of the ESGO API.</summary>
        public const string DefaultBaseUrl = "https://api.es-go.de";

        internal static readonly JsonSerializerOptions Json = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private static readonly string UserAgent = "esgo-dotnet/" + PackageVersion();

        private readonly HttpClient _http;
        private readonly bool _ownsHttp;

        /// <summary>Creates a client.</summary>
        /// <param name="apiKey">API key; falls back to <see cref="EsgoConfiguration.ApiKey"/>, then <c>ESGO_API_KEY</c>.</param>
        /// <param name="baseUrl">Base URL; falls back to <see cref="EsgoConfiguration.BaseUrl"/>.</param>
        /// <param name="httpClient">Your own <see cref="HttpClient"/> (for example from IHttpClientFactory). Not disposed by this client.</param>
        public EsgoClient(string? apiKey = null, string? baseUrl = null, HttpClient? httpClient = null)
        {
            var key = apiKey ?? EsgoConfiguration.ApiKey ?? Environment.GetEnvironmentVariable("ESGO_API_KEY");
            if (string.IsNullOrWhiteSpace(key))
                throw new EsgoException("No API key provided. Pass apiKey, set EsgoConfiguration.ApiKey or the ESGO_API_KEY environment variable.");

            ApiKey = key!;
            BaseUrl = (baseUrl ?? EsgoConfiguration.BaseUrl).TrimEnd('/');
            _http = httpClient ?? new HttpClient();
            _ownsHttp = httpClient is null;

            Me = new MeResource(this);
            Matches = new MatchesResource(this);
            OpenRequests = new OpenRequestsResource(this);
        }

        /// <summary>The API key this client sends.</summary>
        public string ApiKey { get; }

        /// <summary>The base URL this client calls.</summary>
        public string BaseUrl { get; }

        /// <summary>The API key in use and its daily quotas.</summary>
        public MeResource Me { get; }

        /// <summary>Search for trips with free capacity.</summary>
        public MatchesResource Matches { get; }

        /// <summary>Publish open transport requests and read their matches.</summary>
        public OpenRequestsResource OpenRequests { get; }

        internal async Task<T> SendAsync<T>(Operation operation, object? body, IDictionary<string, string>? pathParameters, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(new HttpMethod(operation.Method), BaseUrl + operation.BuildPath(pathParameters));
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + ApiKey);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            if (body != null)
                request.Content = new StringContent(JsonSerializer.Serialize(body, body.GetType(), Json), Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                throw new EsgoConnectionException("Could not reach " + BaseUrl + ": " + ex.Message, ex);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new EsgoConnectionException("Request to " + BaseUrl + " timed out.", ex);
            }

            using (response)
            {
                var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    throw EsgoApiException.FromResponse(response, text);
                return JsonSerializer.Deserialize<T>(text, Json)
                    ?? throw new EsgoException("Empty response from " + operation.Path + ".");
            }
        }

        /// <summary>Disposes the internal <see cref="HttpClient"/> (not one you passed in).</summary>
        public void Dispose()
        {
            if (_ownsHttp)
                _http.Dispose();
        }

        private static string PackageVersion()
        {
            var version = typeof(EsgoClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0";
            var plus = version.IndexOf('+');
            return plus < 0 ? version : version.Substring(0, plus);
        }
    }
}
