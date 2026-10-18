using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Esgo.Models;

namespace Esgo.Resources
{
    /// <summary>The API key in use and its daily quotas.</summary>
    public sealed class MeResource
    {
        private readonly EsgoClient _client;
        internal MeResource(EsgoClient client) => _client = client;

        /// <summary><c>GET /v1/me</c>: key, account role and remaining daily quotas.</summary>
        public Task<Me> RetrieveAsync(CancellationToken cancellationToken = default)
            => _client.SendAsync<Me>(Operations.MeRetrieve, null, null, cancellationToken);
    }

    /// <summary>Search for trips with free capacity that fit a shipment.</summary>
    public sealed class MatchesResource
    {
        private readonly EsgoClient _client;
        internal MatchesResource(EsgoClient client) => _client = client;

        /// <summary>
        /// <c>POST /v1/matches/search</c>. Required: <c>Origin</c>, <c>Destination</c>, <c>WeightKg</c>.
        /// Counts against the daily quota <c>matches_search</c>.
        /// </summary>
        public Task<MatchSearchResult> SearchAsync(MatchSearchRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            return _client.SendAsync<MatchSearchResult>(Operations.MatchesSearch, request, null, cancellationToken);
        }
    }

    /// <summary>Publish open transport requests and read their matches.</summary>
    public sealed class OpenRequestsResource
    {
        private readonly EsgoClient _client;
        internal OpenRequestsResource(EsgoClient client) => _client = client;

        /// <summary>
        /// <c>POST /v1/open-requests</c>. Required: <c>Origin</c>, <c>Destination</c>, <c>WeightKg</c>,
        /// <c>LoadingMeters</c>, <c>CargoType</c>, <c>TimeWindows</c> (local time Europe/Berlin).
        /// Counts against the daily quota <c>open_requests_create</c>.
        /// </summary>
        public Task<OpenRequest> CreateAsync(OpenRequestCreate request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            return _client.SendAsync<OpenRequest>(Operations.OpenRequestsCreate, request, null, cancellationToken);
        }

        /// <summary><c>GET /v1/open-requests/{request_id}/matches</c>: current matches of one of your open requests.</summary>
        public Task<MatchList> ListMatchesAsync(string requestId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(requestId)) throw new ArgumentException("requestId is required.", nameof(requestId));
            return _client.SendAsync<MatchList>(Operations.OpenRequestsListMatches, null,
                new Dictionary<string, string> { ["request_id"] = requestId }, cancellationToken);
        }
    }
}
