namespace Esgo
{
    /// <summary>
    /// Process-wide defaults, like <c>StripeConfiguration.ApiKey</c>. A client created without
    /// an API key uses <see cref="ApiKey"/>, then the <c>ESGO_API_KEY</c> environment variable.
    /// </summary>
    public static class EsgoConfiguration
    {
        /// <summary>Default API key (<c>esgo_live_...</c>).</summary>
        public static string? ApiKey { get; set; }

        /// <summary>Default base URL of the API.</summary>
        public static string BaseUrl { get; set; } = EsgoClient.DefaultBaseUrl;
    }
}
