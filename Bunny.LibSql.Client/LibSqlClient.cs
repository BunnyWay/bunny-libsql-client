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
        private static HttpClient _httpClient = new();
        private string _accessToken = string.Empty;
        private string _baseUrl = string.Empty;
        
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
            
            _accessToken = accessToken;
            _baseUrl = baseUrl;
            
            // Remove trailing slash
            while(_baseUrl.EndsWith("/"))
            {
                _baseUrl = _baseUrl[..^1];
            }
        }

        private HttpRequestMessage CreateGetRequest(string requestUri)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/{requestUri}");
            req.Headers.Add("Authorization", $"Bearer {_accessToken}");
            
            return req;
        }
        
        private HttpRequestMessage CreateHttpPostRequest(string json, string requestUri)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}{requestUri}");
            req.Headers.Add("Authorization", $"Bearer {_accessToken}");
            
            var content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            req.Content = content;
            
            return req;
        }
    }
}