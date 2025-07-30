using System.Globalization;
using System.Net;
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
        public Task<PipelineResponse?> QueryAsync(SqlQuery reqqueryuest, CancellationToken cancellationToken = default) 
            => QueryMultipleAsync([reqqueryuest], cancellationToken);

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) 
            => QueryAsync("BEGIN TRANSACTION", null, cancellationToken);
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) 
            => QueryAsync("COMMIT TRANSACTION", null, cancellationToken);
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) 
            => QueryAsync("ROLLBACK TRANSACTION", null, cancellationToken);
        
        public async Task<PipelineResponse?> QueryMultipleAsync(List<SqlQuery> queries, CancellationToken cancellationToken = default)
        {
            var postJson = CreatePipelinecallAsJson(queries);
            string? receiveJson = null;
            try
            {
                using var req = CreateHttpPostRequest(postJson, "/v2/pipeline");
                using var response = await _client.SendAsync(req, cancellationToken);
                receiveJson = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    try
                    {
                        
                        var responseError = JsonSerializer.Deserialize<ResponseError>(receiveJson);
                        if (responseError != null)
                        {
                            throw new LibSqlClientException(responseError.Message, postJson, receiveJson, null, null);
                        }
                    }
                    catch (JsonException)
                    {
                        // Ignore
                    }
                }
                
                var pipelineResponse = JsonSerializer.Deserialize<PipelineResponse>(receiveJson);
                ProcessResponseBaton(pipelineResponse!);
                CheckResponseForErrors(queries.Select(e => e.sql).ToArray(), pipelineResponse);
                return pipelineResponse;
            }
            catch (Exception e)
            {
                throw new LibSqlClientException(e.Message, postJson, receiveJson, null, e);
            }
        }
  
        
        public async Task<PipelineResponse?> QueryAsync(string query, IEnumerable<object>? args = null, CancellationToken cancellationToken = default) => await QueryAsync(new SqlQuery(query, args?.ToArray()), cancellationToken);

 
        
        public async Task<List<T>> QueryAsync<T>(string query, IEnumerable<object>? args = null, List<JoinNavigation>? joins = null, CancellationToken cancellationToken = default)
        {
            var dbResponse = await QueryAsync(new SqlQuery(query, args?.ToArray()), cancellationToken);
            var result = dbResponse?.Results?.FirstOrDefault()?.Response?.Result;
            if (result == null)
            {
                throw new LibSqlClientException("No response from server", null, null, query, null);
            }
            
            var mapped = LibSqlResultMapper.Map<T>(result.Cols, result.Rows, joins);
            return mapped;
        }

        public async Task<T?> QueryOneAsync<T>(string query, IEnumerable<object>? args = null,
            List<JoinNavigation>? joins = null, CancellationToken cancellationToken = default)
        {
            var results = await QueryAsync<T>(query, args, joins, cancellationToken);
            return results.FirstOrDefault();
        }

        private void CheckResponseForErrors(string sqlQuery, PipelineResponse? response) =>
            CheckResponseForErrors([sqlQuery], response);

        private void CheckResponseForErrors(string[] sqlQueries, PipelineResponse? response)
        {
            if(response == null || response.Results == null)
            {
                throw new LibSqlException("", "An unknown error has occured while executing the query");
            }

            int queryIndex = 0;
            foreach (var result in response.Results)
            {
                if (result.Type == PipelineResultType.Error)
                {
                    if (result.Error == null)
                    {
                        throw new LibSqlException(sqlQueries[queryIndex], "An unknown error has occured while executing the query");
                    }
                    
                    throw new LibSqlException(sqlQueries[queryIndex], result.Error.Message);
                }

                queryIndex++;
            }
        }
        
        public async Task<T> ExecuteScalarAsync<T>(string query, IEnumerable<object>? args = null, CancellationToken cancellationToken = default)
        {
            var dbResponse = await QueryAsync(query, args, cancellationToken);
            var result = dbResponse!.Results!.FirstOrDefault()!.Response!.Result!; // this is already error checked
            if (result == null)
            {
                throw new LibSqlClientException("No response from server", null, null, query, null);
            }
            if (result.Rows.Count == 0)
            {
                throw new LibSqlClientException("No rows returned", null, null, query, null);
            }
            if (result.Rows[0].Count == 0)
            {
                throw new LibSqlClientException("No columns returned", null, null, query, null);
            }
            
            var firstRow = result.Rows[0];
            var firstColumn = firstRow[0];
            if (firstColumn == null)
            {
                throw new LibSqlClientException("Null value returned", null, null, query, null);
            }
            
            if (firstColumn.Type == LibSqlValueType.Null)
            {
                // return 0 in the correct type
                if (typeof(T) == typeof(long))
                {
                    return (T)(object)0L;
                }
                if (typeof(T) == typeof(int))
                {
                    return (T)(object)0;
                }
                if (typeof(T) == typeof(double))
                {
                    return (T)(object)0.0;
                }
                if (typeof(T) == typeof(float))
                {
                    return (T)(object)0.0f;
                }
            }
            
            if (firstColumn.Type == LibSqlValueType.Integer)
            {
                if (typeof(T) == typeof(long))
                {
                    if (!long.TryParse(firstColumn.Value.ToString(), CultureInfo.InvariantCulture, out var value))
                    {
                        throw new LibSqlClientException("Invalid value returned", null, null, query, null);
                    }
                    return (T)(object)value;
                }
                if (typeof(T) == typeof(int))
                {
                    if (!int.TryParse(firstColumn.Value.ToString(), CultureInfo.InvariantCulture, out var value))
                    {
                        throw new LibSqlClientException("Invalid value returned", null, null, query, null);
                    }
                    return (T)(object)value;
                }
            }
            
            if (firstColumn.Type == LibSqlValueType.Float)
            {
                if (typeof(T) == typeof(double))
                {
                    if (!double.TryParse(firstColumn.Value.ToString(), CultureInfo.InvariantCulture, out var value))
                    {
                        throw new LibSqlClientException("Invalid value returned", null, null, query, null);
                    }
                    return (T)(object)value;
                }
                if (typeof(T) == typeof(float))
                {
                    if (!float.TryParse(firstColumn.Value.ToString(), CultureInfo.InvariantCulture, out var value))
                    {
                        throw new LibSqlClientException("Invalid value returned", null, null, query, null);
                    }
                    return (T)(object)value;
                }
            }
            
            throw new LibSqlClientException("Invalid type returned", null, null, query, null);
        }
        public async Task<long> ExecuteScalarAsync(string query, IEnumerable<object>? args = null,
            CancellationToken cancellationToken = default) => await ExecuteScalarAsync<long>(query, args, cancellationToken);
    }
}