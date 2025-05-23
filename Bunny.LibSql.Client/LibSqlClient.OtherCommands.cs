using System.Globalization;
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
    public partial class LibSqlClient
    {
        public async Task<string> GetDatabaseVersionAsync(CancellationToken cancellationToken = default)
        {
            return await _client.GetStringAsync("/version", cancellationToken);
        }

        public async Task DumpDatabaseToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "/dump");
            using var response = await _client.SendAsync(req, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                await response.Content.CopyToAsync(stream, cancellationToken);
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new LibSqlException("/dump", $"Failed to dump database with error code {response.StatusCode}: {errorMessage}");
            }
        }
    }
}