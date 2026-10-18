using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Esgo.Internal;
using Esgo.Models;

namespace Esgo
{
    /// <summary>Base class for all errors raised by this package.</summary>
    public class EsgoException : Exception
    {
        /// <summary>Creates the exception.</summary>
        public EsgoException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    /// <summary>The API could not be reached (network error, timeout).</summary>
    public sealed class EsgoConnectionException : EsgoException
    {
        /// <summary>Creates the exception.</summary>
        public EsgoConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// The API answered with an error status. Carries the fields of the ESGO error envelope
    /// <c>{"error": {"type", "code", "message", "param"}}</c>.
    /// </summary>
    public class EsgoApiException : EsgoException
    {
        /// <summary>Creates the exception.</summary>
        protected internal EsgoApiException(int status, string message) : base("[" + status + "] " + message)
        {
            Status = status;
            ErrorMessage = message;
        }

        /// <summary>HTTP status code.</summary>
        public int Status { get; }

        /// <summary>Human-readable message from the API.</summary>
        public string ErrorMessage { get; }

        /// <summary>Error type, e.g. <c>rate_limit_error</c>.</summary>
        public string? Type { get; private set; }

        /// <summary>Machine-readable code, e.g. <c>daily_quota_exceeded</c>.</summary>
        public string? Code { get; private set; }

        /// <summary>The request field that caused the error, e.g. <c>time_windows.0.end</c>.</summary>
        public string? Param { get; private set; }

        /// <summary>Value of the <c>X-Request-Id</c> header; quote it when contacting ESGO.</summary>
        public string? RequestId { get; private set; }

        /// <summary>Seconds to wait before retrying (429).</summary>
        public int? RetryAfterSeconds { get; private set; }

        /// <summary>Raw response body.</summary>
        public string? Body { get; private set; }

        internal static EsgoApiException FromResponse(HttpResponseMessage response, string body)
        {
            ErrorBody? error = null;
            try
            {
                error = JsonSerializer.Deserialize<Error>(body, EsgoClient.Json)?.Error1;
            }
            catch (JsonException)
            {
                // Not the ESGO envelope (e.g. a proxy page); status and body still tell the story.
            }

            var status = (int)response.StatusCode;
            var message = error?.Message ?? response.ReasonPhrase ?? "Unexpected response";
            EsgoApiException exception = status switch
            {
                400 or 422 => new InvalidRequestException(status, message),
                401 => new AuthenticationException(status, message),
                403 => new PermissionDeniedException(status, message),
                404 => new NotFoundException(status, message),
                429 => new RateLimitException(status, message),
                503 => new ServiceUnavailableException(status, message),
                >= 500 => new InternalServerException(status, message),
                _ => new EsgoApiException(status, message),
            };

            exception.Type = error is null ? null : EnumMemberConverter<ErrorBodyType>.TextOf(error.Type);
            exception.Code = error?.Code;
            exception.Param = error?.Param;
            exception.Body = body;
            exception.RequestId = response.Headers.TryGetValues("X-Request-Id", out var ids) ? ids.FirstOrDefault() : null;
            var retryAfter = response.Headers.RetryAfter?.Delta;
            exception.RetryAfterSeconds = retryAfter.HasValue ? (int)retryAfter.Value.TotalSeconds : (int?)null;
            return exception;
        }
    }

    /// <summary>400 or 422: the request was malformed or a field is invalid (see <see cref="EsgoApiException.Param"/>).</summary>
    public sealed class InvalidRequestException : EsgoApiException { internal InvalidRequestException(int s, string m) : base(s, m) { } }

    /// <summary>401: the API key is missing, unknown, revoked or expired.</summary>
    public sealed class AuthenticationException : EsgoApiException { internal AuthenticationException(int s, string m) : base(s, m) { } }

    /// <summary>403: the key lacks a scope, the account is disabled or has no subscription.</summary>
    public sealed class PermissionDeniedException : EsgoApiException { internal PermissionDeniedException(int s, string m) : base(s, m) { } }

    /// <summary>404: the resource does not exist or does not belong to you.</summary>
    public sealed class NotFoundException : EsgoApiException { internal NotFoundException(int s, string m) : base(s, m) { } }

    /// <summary>429: rate limit or daily quota exceeded; wait <see cref="EsgoApiException.RetryAfterSeconds"/>.</summary>
    public sealed class RateLimitException : EsgoApiException { internal RateLimitException(int s, string m) : base(s, m) { } }

    /// <summary>503: the API is switched off or temporarily unavailable.</summary>
    public sealed class ServiceUnavailableException : EsgoApiException { internal ServiceUnavailableException(int s, string m) : base(s, m) { } }

    /// <summary>Other 5xx: an error on the ESGO side.</summary>
    public sealed class InternalServerException : EsgoApiException { internal InternalServerException(int s, string m) : base(s, m) { } }
}
