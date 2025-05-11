using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;
using Bunny.LibSql.Client.LINQ;
using Bunny.LibSql.Client.SQL;

namespace Bunny.LibSql.Client
{
    // TODO: implement baton for transaction handling
    // TODO: add metrics and statistics
    // TODO: add query error handling
    // TODO: add logging
    // TODO: add support for turso /dump
    // TODO: add support for turso /health
    // TODO: add support for turso /version
    // TODO: add support for turso /beta/listen
    public partial class LibSqlClient
    {
        private HttpClient _client = new();

        public LibSqlClient(string baseUrl, string accessToken)
        {
            if(baseUrl.StartsWith("libsql://"))
                baseUrl = baseUrl.Replace("libsql://", "https://");

            // Validate base URL
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out _) == false)
            {
                throw new ArgumentException("Invalid base URL", nameof(baseUrl));
            }
            
            // TODO: check what to do with the connections
            _client.BaseAddress = new Uri(baseUrl);
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        }

        private HttpRequestMessage CreateHttpPostRequest(string json, string requestUri)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, requestUri);
            var content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            req.Content = content;
            return req;
        }
    }
}