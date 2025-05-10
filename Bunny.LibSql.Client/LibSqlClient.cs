using System.Net.Http.Json;
using System.Text.Json;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;
using Bunny.LibSql.Client.LINQ;
using Bunny.LibSql.Client.SQL;

namespace Bunny.LibSql.Client
{
    // TODO: implement baton for transaction handling
    // TODO: add metrics and statistics
    // TODO: add error handling
    // TODO: add logging
    // TODO: add cancellation token support
    // TODO: add support for turso /dump
    // TODO: add support for turso /health
    // TODO: add support for turso /version
    public class LibSqlClient
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

        public Task<PipelineResponse?> QueryAsync(SqlQuery reqqueryuest) => QueryAsync(reqqueryuest.Sql, reqqueryuest.Args);

        public async Task<PipelineResponse?> QueryMultipleAsync(List<SqlQuery> queries)
        {
            var call = new PipelineCall();
            foreach (var query in queries)   
            {
                call.Requests.Add(new PipelineRequest()
                {
                    Stmt = new Statement()
                    {
                        Sql = query.sql,
                        Args = GetArgs(query.args),
                    },
                    Type = PipelineRequestType.Execute,
                });
            }
            
            var serialize = JsonSerializer.Serialize(call);
            using var response = await _client.PostAsJsonAsync("/v2/pipeline", call);
            var json = await response.Content.ReadAsStringAsync();
            try
            {

                return JsonSerializer.Deserialize<PipelineResponse>(json);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        
        public async Task<PipelineResponse?> QueryAsync(string query, IEnumerable<object>? args = null)
        {
            // TODO: clean this up
            var call = new PipelineCall();
            call.Requests.Add(new PipelineRequest()
            {
                Stmt = new Statement()
                {
                    Sql = query,
                    Args = GetArgs(args),
                },
                Type = PipelineRequestType.Execute,
            });
            
            var serialize = JsonSerializer.Serialize(call); // TODO remove this, it's only used for debugging
            using var response = await _client.PostAsJsonAsync("/v2/pipeline", call);
            var json = await response.Content.ReadAsStringAsync();
            // TODO: remove this, it's only used for debuggin
            try
            {
                return JsonSerializer.Deserialize<PipelineResponse>(json);
            }
            catch (Exception e)
            {
                // TODO: add error handling
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<List<T>> QueryAsync<T>(string query, IEnumerable<object>? args = null, List<JoinNavigation>? joins = null)
        {
            var dbResponse = await QueryAsync(query, args);
            var result = dbResponse.Results.FirstOrDefault().Response.Result;
            var mapped = LibSqlResultMapper.Map<T>(result.Cols, result.Rows, joins);
            return mapped;
        }
        
        // TODO: execute scalar
        
        // TODO: query one

        
        // TODO: move this somewhere else for cleaner code
        private List<LibSqlValue>? GetArgs(IEnumerable<object>? args)
        {
            // Avoid allocating a list if there are no args
            if (args == null || !args.Any())
                return [];
            
            var libSqlValues = new List<LibSqlValue>();
            foreach (var arg in args)
            {
                // TODO: what's that?
                if (!arg.GetType().IsLibSqlSupportedType())
                {
                    continue;
                }
                
                if (arg is LibSqlValue libSqlValue)
                {
                    libSqlValues.Add(libSqlValue);
                }
                else
                {
                    if (arg is double d)
                    {
                        libSqlValues.Add(new LibSqlValue()
                        {
                            Type = LibSqlValueType.Float,
                            Value = d
                        });
                    }
                    else if (arg is double f)
                    {
                        libSqlValues.Add(new LibSqlValue()
                        {
                            Type = LibSqlValueType.Float,
                            Value = f
                        });
                    }
                    else if (arg is int i)
                    {
                        libSqlValues.Add(new LibSqlValue()
                        {
                            Type = LibSqlValueType.Integer,
                            Value = i.ToString()
                        });
                    }
                    else if (arg is long l)
                    {
                        libSqlValues.Add(new LibSqlValue()
                        {
                            Type = LibSqlValueType.Integer,
                            Value = l.ToString()
                        });
                    }
                    else
                    {
                        libSqlValues.Add(new LibSqlValue()
                        {
                            Type = LibSqlValueType.Text,
                            Value = arg.ToString()
                        });
                    }
                }
            }
            
            return libSqlValues;
        }
        
    }
}