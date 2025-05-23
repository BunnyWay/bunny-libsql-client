using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bunny.LibSql.Client;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.HttpClientModels.Enums;
using Bunny.LibSql.Client.Tests; // For HttpClientMockHelper

namespace Bunny.LibSql.Client.Tests
{
    [TestFixture]
    public class LibSqlClientErrorHandlingTests
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters = { new LibSqlValue.LibSqlValueConverter() }
        };

        private LibSqlClient CreateClientWithMockResponse(
            string responseContent, 
            HttpStatusCode statusCode = HttpStatusCode.OK, 
            Action<HttpRequestMessage>? requestCallback = null)
        {
            var responseMessage = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json") // Assume JSON for most, can be overridden
            };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage, requestCallback);
            return new LibSqlClient("http://localhost", httpClient);
        }

        private LibSqlClient CreateClientWithMockResponse(
            PipelineResponse pipelineResponse, 
            HttpStatusCode statusCode = HttpStatusCode.OK,
            Action<HttpRequestMessage>? requestCallback = null)
        {
            var responseJson = JsonSerializer.Serialize(pipelineResponse, _jsonOptions);
            return CreateClientWithMockResponse(responseJson, statusCode, requestCallback);
        }

        // LibSqlException Tests
        [Test]
        public async Task QueryAsync_PipelineResultError_ThrowsLibSqlExceptionWithDetails()
        {
            // Arrange
            var errorMessage = "Table 'users' not found";
            var query = "SELECT * FROM users";
            var pipelineResponse = new PipelineResponse
            {
                Results = new List<PipelineResult>
                {
                    new PipelineResult
                    {
                        Type = PipelineResultType.Error,
                        Error = new PipelineResultError { Message = errorMessage }
                    }
                }
            };
            var client = CreateClientWithMockResponse(pipelineResponse);

            // Act & Assert
            var ex = Assert.ThrowsAsync<LibSqlException>(async () => await client.QueryAsync(query));
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.Message, Does.Contain(errorMessage));
            Assert.That(ex.OriginalQuery, Is.EqualTo(query));
        }

        [Test]
        public async Task QueryMultipleAsync_OneResultError_ThrowsLibSqlExceptionWithDetails()
        {
            // Arrange
            var errorMessage = "No such table: non_existent_table";
            var queries = new List<SqlQuery>
            {
                new SqlQuery("SELECT 1"),
                new SqlQuery("INSERT INTO non_existent_table VALUES (1)")
            };
            var pipelineResponse = new PipelineResponse
            {
                Results = new List<PipelineResult>
                {
                    new PipelineResult { Type = PipelineResultType.Ok, Response = new QueryResult { Type = QueryResponseType.None } },
                    new PipelineResult { Type = PipelineResultType.Error, Error = new PipelineResultError { Message = errorMessage } }
                }
            };
            var client = CreateClientWithMockResponse(pipelineResponse);

            // Act & Assert
            var ex = Assert.ThrowsAsync<LibSqlException>(async () => await client.QueryMultipleAsync(queries));
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.Message, Does.Contain(errorMessage));
            Assert.That(ex.OriginalQuery, Is.EqualTo("INSERT INTO non_existent_table VALUES (1)"));
        }

        // LibSqlClientException Tests
        [Test]
        public async Task QueryAsync_InvalidJsonResponse_ThrowsLibSqlClientException()
        {
            // Arrange
            var invalidJson = "this is not json";
            var query = "SELECT 1";
            HttpRequestMessage? capturedRequest = null;
            var client = CreateClientWithMockResponse(invalidJson, requestCallback: req => capturedRequest = req);

            // Act & Assert
            var ex = Assert.ThrowsAsync<LibSqlClientException>(async () => await client.QueryAsync(query));
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.ResponseBody, Is.EqualTo(invalidJson));
            Assert.That(ex.OriginalQuery, Is.EqualTo(query));
            Assert.That(ex.Message, Does.Contain("Error deserializing HTTP response"));

            if (capturedRequest != null)
            {
                var requestBody = await capturedRequest.Content.ReadAsStringAsync();
                Assert.That(ex.RequestBody, Is.EqualTo(requestBody));
            }
            else
            {
                Assert.Fail("Request was not captured to verify RequestBody.");
            }
        }

        [Test]
        public async Task QueryAsync_ResponseMissingResults_ThrowsLibSqlClientException()
        {
            // Arrange
            var malformedJson = "{ \"baton\": \"abc\" }"; // Valid JSON, but missing 'results'
            var query = "SELECT 1";
            HttpRequestMessage? capturedRequest = null;
            var client = CreateClientWithMockResponse(malformedJson, requestCallback: req => capturedRequest = req);
            
            // Act & Assert
            var ex = Assert.ThrowsAsync<LibSqlClientException>(async () => await client.QueryAsync(query));
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.ResponseBody, Is.EqualTo(malformedJson));
            Assert.That(ex.OriginalQuery, Is.EqualTo(query));
            Assert.That(ex.Message, Does.Contain("Response structure is invalid. 'Results' property is missing or null."));

            if (capturedRequest != null)
            {
                var requestBody = await capturedRequest.Content.ReadAsStringAsync();
                Assert.That(ex.RequestBody, Is.EqualTo(requestBody));
            }
            else
            {
                Assert.Fail("Request was not captured to verify RequestBody.");
            }
        }

        [Test]
        public async Task ExecuteScalarAsync_NoRows_ThrowsLibSqlClientException()
        {
            // Arrange
            var query = "SELECT COUNT(*) FROM empty_table";
            var pipelineResponse = new PipelineResponse
            {
                Results = new List<PipelineResult>
                {
                    new PipelineResult
                    {
                        Type = PipelineResultType.Ok,
                        Response = new QueryResult
                        {
                            Type = QueryResponseType.Row,
                            Cols = new List<QueryCol> { new QueryCol { Name = "COUNT(*)" } },
                            Rows = new List<List<LibSqlValue>>() // Empty rows
                        }
                    }
                }
            };
            HttpRequestMessage? capturedRequest = null;
            var client = CreateClientWithMockResponse(pipelineResponse, requestCallback: req => capturedRequest = req);

            // Act & Assert
            var ex = Assert.ThrowsAsync<LibSqlClientException>(async () => await client.ExecuteScalarAsync<long>(query));
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.Message, Does.Contain("Query returned no rows, cannot execute scalar."));
            Assert.That(ex.OriginalQuery, Is.EqualTo(query));
            Assert.That(ex.ResponseBody, Is.EqualTo(JsonSerializer.Serialize(pipelineResponse, _jsonOptions)));
            
            if (capturedRequest != null)
            {
                var requestBody = await capturedRequest.Content.ReadAsStringAsync();
                Assert.That(ex.RequestBody, Is.EqualTo(requestBody));
            }
            else
            {
                Assert.Fail("Request was not captured to verify RequestBody.");
            }
        }

        [Test]
        public async Task ExecuteScalarAsync_NoCols_ThrowsLibSqlClientException()
        {
            // Arrange
            var query = "SELECT FROM no_cols_table"; // Query doesn't really matter for the mock
            var pipelineResponse = new PipelineResponse
            {
                Results = new List<PipelineResult>
                {
                    new PipelineResult
                    {
                        Type = PipelineResultType.Ok,
                        Response = new QueryResult
                        {
                            Type = QueryResponseType.Row,
                            Cols = new List<QueryCol>(), // Empty columns
                            Rows = new List<List<LibSqlValue>> { new List<LibSqlValue>() } // One row, but no cells due to no cols
                        }
                    }
                }
            };
            HttpRequestMessage? capturedRequest = null;
            var client = CreateClientWithMockResponse(pipelineResponse, requestCallback: req => capturedRequest = req);

            // Act & Assert
            var ex = Assert.ThrowsAsync<LibSqlClientException>(async () => await client.ExecuteScalarAsync<long>(query));
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.Message, Does.Contain("Query result has no columns or first row has no cells, cannot execute scalar."));
            Assert.That(ex.OriginalQuery, Is.EqualTo(query));
            Assert.That(ex.ResponseBody, Is.EqualTo(JsonSerializer.Serialize(pipelineResponse, _jsonOptions)));

            if (capturedRequest != null)
            {
                var requestBody = await capturedRequest.Content.ReadAsStringAsync();
                Assert.That(ex.RequestBody, Is.EqualTo(requestBody));
            }
            else
            {
                Assert.Fail("Request was not captured to verify RequestBody.");
            }
        }
    }
}
