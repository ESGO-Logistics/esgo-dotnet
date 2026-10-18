using System;
using System.Collections.Generic;
using Esgo.Models;

namespace Esgo
{
    /// <summary>
    /// The one place that maps an operationId of the spec to its HTTP method, path, models and
    /// SDK method. tests/Esgo.Tests/OperationsTests.cs checks it against openapi.json, so a new
    /// endpoint in the spec fails the tests until it is added here and in Resources/.
    /// </summary>
    internal sealed class Operation
    {
        public Operation(string operationId, string method, string path, Type? request, Type response, string sdkMethod)
        {
            OperationId = operationId;
            Method = method;
            Path = path;
            Request = request;
            Response = response;
            SdkMethod = sdkMethod;
        }

        public string OperationId { get; }
        public string Method { get; }
        public string Path { get; }
        public Type? Request { get; }
        public Type Response { get; }

        /// <summary>Resource property and method on <see cref="EsgoClient"/>, e.g. "Matches.SearchAsync".</summary>
        public string SdkMethod { get; }

        public string BuildPath(IDictionary<string, string>? parameters)
        {
            var path = Path;
            if (parameters != null)
                foreach (var pair in parameters)
                    path = path.Replace("{" + pair.Key + "}", Uri.EscapeDataString(pair.Value));
            return path;
        }
    }

    internal static class Operations
    {
        public static readonly Operation MeRetrieve =
            new Operation("me_retrieve", "GET", "/v1/me", null, typeof(Me), "Me.RetrieveAsync");

        public static readonly Operation MatchesSearch =
            new Operation("matches_search", "POST", "/v1/matches/search", typeof(MatchSearchRequest), typeof(MatchSearchResult), "Matches.SearchAsync");

        public static readonly Operation OpenRequestsCreate =
            new Operation("open_requests_create", "POST", "/v1/open-requests", typeof(OpenRequestCreate), typeof(OpenRequest), "OpenRequests.CreateAsync");

        public static readonly Operation OpenRequestsListMatches =
            new Operation("open_requests_list_matches", "GET", "/v1/open-requests/{request_id}/matches", null, typeof(MatchList), "OpenRequests.ListMatchesAsync");

        public static readonly IReadOnlyList<Operation> All = new[]
        {
            MeRetrieve, MatchesSearch, OpenRequestsCreate, OpenRequestsListMatches,
        };
    }
}
