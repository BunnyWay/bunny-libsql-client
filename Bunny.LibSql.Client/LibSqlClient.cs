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
    // TODO: add logging
    public partial class LibSqlClient
    {
        private HttpClient _client = new();
        public string? Baton { get; set; } = null;
        
        public LibSqlClient(string baseUrl, string accessToken)
        {
            if(baseUrl.StartsWith("libsql://"))
                baseUrl = baseUrl.Replace("libsql://", "https://");

            // Validate base URL
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out _) == false)
            {
                throw new ArgumentException("Invalid base URL", nameof(baseUrl));
            }
            
            // TODO: move this out so we can have multiple clients active, but reusing the same HttpClient instance
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
        
        private void ProcessResponseBaton(PipelineResponse response)
        {
            Baton = response.Baton;
        }
    }
}