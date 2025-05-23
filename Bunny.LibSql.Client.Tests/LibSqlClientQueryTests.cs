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
    public class LibSqlClientQueryTests
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters = { new LibSqlValue.LibSqlValueConverter() } // Ensure this converter is used
        };

        // QueryAsync (string query) Tests
        [Test]
        public async Task QueryAsync_SimpleSelect_ReturnsSuccessResponse()
        {
            // Arrange
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
                            Cols = new List<QueryCol> { new QueryCol { Name = "1" } },
                            Rows = new List<List<LibSqlValue>>
                            {
                                new List<LibSqlValue> { new LibSqlValue { Type = LibSqlValueType.Integer, Value = "1" } }
                            }
                        }
                    }
                }
            };
            var responseJson = JsonSerializer.Serialize(pipelineResponse, _jsonOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage);
            var client = new LibSqlClient("http://localhost", httpClient);

            // Act
            var result = await client.QueryAsync("SELECT 1");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Results, Is.Not.Null);
            Assert.That(result.Results.Count, Is.EqualTo(1));
            Assert.That(result.Results[0].Type, Is.EqualTo(PipelineResultType.Ok));
            Assert.That(result.Results[0].Error, Is.Null);
        }

        [Test]
        public void QueryAsync_ServerError_ThrowsLibSqlException()
        {
            // Arrange
            var errorMessage = "Syntax error";
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
            var responseJson = JsonSerializer.Serialize(pipelineResponse, _jsonOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) // HTTP call itself is OK
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage);
            var client = new LibSqlClient("http://localhost", httpClient);

            // Assert
            var ex = Assert.ThrowsAsync<LibSqlException>(async () => await client.QueryAsync("SELECT INVALID"));
            Assert.That(ex.Message, Does.Contain(errorMessage));
        }

        // ExecuteScalarAsync<T> Tests
        [Test]
        [TestCase(123L, LibSqlValueType.Integer, "123")]
        [TestCase(456, LibSqlValueType.Integer, "456")]
        [TestCase(78.9, LibSqlValueType.Real, "78.9")]
        [TestCase(10.11f, LibSqlValueType.Real, "10.11")]
        public async Task ExecuteScalarAsync_SuccessfulQuery_ReturnsCorrectType<T>(T expectedValue, LibSqlValueType valueType, string rawValue)
        {
            // Arrange
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
                            Cols = new List<QueryCol> { new QueryCol { Name = "value" } },
                            Rows = new List<List<LibSqlValue>>
                            {
                                new List<LibSqlValue> { new LibSqlValue { Type = valueType, Value = rawValue } }
                            }
                        }
                    }
                }
            };
            var responseJson = JsonSerializer.Serialize(pipelineResponse, _jsonOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage);
            var client = new LibSqlClient("http://localhost", httpClient);

            // Act
            var result = await client.ExecuteScalarAsync<T>("SELECT value");

            // Assert
            Assert.That(result, Is.EqualTo(expectedValue));
        }

        [Test]
        public void ExecuteScalarAsync_NoRows_ThrowsLibSqlClientException()
        {
            // Arrange
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
                            Cols = new List<QueryCol> { new QueryCol { Name = "value" } },
                            Rows = new List<List<LibSqlValue>>() // Empty rows
                        }
                    }
                }
            };
            var responseJson = JsonSerializer.Serialize(pipelineResponse, _jsonOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage);
            var client = new LibSqlClient("http://localhost", httpClient);

            // Assert
            var ex = Assert.ThrowsAsync<LibSqlClientException>(async () => await client.ExecuteScalarAsync<long>("SELECT value"));
            Assert.That(ex.Message, Does.Contain("Query returned no rows"));
        }
        
        [Test]
        [TestCase(typeof(long), 0L)]
        [TestCase(typeof(int), 0)]
        [TestCase(typeof(double), 0.0)]
        [TestCase(typeof(float), 0.0f)]
        [TestCase(typeof(string), null)]
        public async Task ExecuteScalarAsync_NullValue_ReturnsDefault<T>(Type type, T expectedDefault)
        {
            // Arrange
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
                            Cols = new List<QueryCol> { new QueryCol { Name = "value" } },
                            Rows = new List<List<LibSqlValue>>
                            {
                                new List<LibSqlValue> { new LibSqlValue { Type = LibSqlValueType.Null } }
                            }
                        }
                    }
                }
            };
            var responseJson = JsonSerializer.Serialize(pipelineResponse, _jsonOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage);
            var client = new LibSqlClient("http://localhost", httpClient);

            // Act
            var result = await client.ExecuteScalarAsync<T>("SELECT NULL");

            // Assert
            Assert.That(result, Is.EqualTo(expectedDefault));
        }


        // Transaction Commands Tests
        [Test]
        [TestCase("BEGIN TRANSACTION")]
        [TestCase("COMMIT TRANSACTION")]
        [TestCase("ROLLBACK TRANSACTION")]
        public async Task TransactionCommands_SendsCorrectSql(string expectedSql)
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            var pipelineResponse = new PipelineResponse // Simple success response
            {
                Results = new List<PipelineResult> { new PipelineResult { Type = PipelineResultType.Ok, Response = new QueryResult { Type = QueryResponseType.None } } }
            };
            var responseJson = JsonSerializer.Serialize(pipelineResponse, _jsonOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };

            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage, req => capturedRequest = req);
            var client = new LibSqlClient("http://localhost", httpClient);

            // Act
            switch (expectedSql)
            {
                case "BEGIN TRANSACTION":
                    await client.BeginTransactionAsync();
                    break;
                case "COMMIT TRANSACTION":
                    await client.CommitTransactionAsync();
                    break;
                case "ROLLBACK TRANSACTION":
                    await client.RollbackTransactionAsync();
                    break;
                default:
                    Assert.Fail($"Unknown SQL command for test: {expectedSql}");
                    break;
            }

            // Assert
            Assert.That(capturedRequest, Is.Not.Null, "HTTP request was not captured.");
            Assert.That(capturedRequest.Content, Is.Not.Null, "HTTP request content is null.");
            
            var requestBody = await capturedRequest.Content.ReadAsStringAsync();
            var pipelineCall = JsonSerializer.Deserialize<PipelineCall>(requestBody, _jsonOptions);

            Assert.That(pipelineCall, Is.Not.Null, "Could not deserialize request body to PipelineCall.");
            Assert.That(pipelineCall.Requests, Is.Not.Null, "PipelineCall.Requests is null.");
            Assert.That(pipelineCall.Requests.Count, Is.EqualTo(1), "PipelineCall.Requests should have one request.");
            Assert.That(pipelineCall.Requests[0].Type, Is.EqualTo(PipelineRequestType.Execute), "Request type should be Execute.");
            Assert.That(pipelineCall.Requests[0].Statement, Is.Not.Null, "Statement is null.");
            Assert.That(pipelineCall.Requests[0].Statement.Sql, Is.EqualTo(expectedSql));
        }

        // Argument Mapping Tests (via QueryAsync)
        public static IEnumerable<TestCaseData> ArgumentTypeTestCases
        {
            get
            {
                yield return new TestCaseData(123, new LibSqlValue { Type = LibSqlValueType.Integer, Value = "123" }).SetName("QueryAsync_WithIntArgument_MapsToInteger");
                yield return new TestCaseData(1234567890L, new LibSqlValue { Type = LibSqlValueType.Integer, Value = "1234567890" }).SetName("QueryAsync_WithLongArgument_MapsToInteger");
                yield return new TestCaseData(123.45d, new LibSqlValue { Type = LibSqlValueType.Real, Value = "123.45" }).SetName("QueryAsync_WithDoubleArgument_MapsToReal"); // JSON will store as number
                yield return new TestCaseData(12.3f, new LibSqlValue { Type = LibSqlValueType.Real, Value = "12.3" }).SetName("QueryAsync_WithFloatArgument_MapsToReal");   // JSON will store as number
                yield return new TestCaseData("hello", new LibSqlValue { Type = LibSqlValueType.Text, Value = "hello" }).SetName("QueryAsync_WithStringArgument_MapsToText");
                yield return new TestCaseData(new byte[] { 0x01, 0x02 }, new LibSqlValue { Type = LibSqlValueType.Blob, Value = "AQI=" }).SetName("QueryAsync_WithByteArrayArgument_MapsToBlob");
                yield return new TestCaseData(true, new LibSqlValue { Type = LibSqlValueType.Integer, Value = "1" }).SetName("QueryAsync_WithTrueBoolArgument_MapsToInteger1");
                yield return new TestCaseData(false, new LibSqlValue { Type = LibSqlValueType.Integer, Value = "0" }).SetName("QueryAsync_WithFalseBoolArgument_MapsToInteger0");
                var selfValue = new LibSqlValue { Type = LibSqlValueType.Text, Value = "self" };
                yield return new TestCaseData(selfValue, selfValue).SetName("QueryAsync_WithLibSqlValueArgument_PassesThrough");
            }
        }

        [Test]
        [TestCaseSource(nameof(ArgumentTypeTestCases))]
        public async Task QueryAsync_WithVariousArgumentTypes_MapsCorrectly(object argumentValue, LibSqlValue expectedLibSqlValue)
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            var mockPipelineResponse = new PipelineResponse // Minimal success response
            {
                Results = new List<PipelineResult> { new PipelineResult { Type = PipelineResultType.Ok, Response = new QueryResult { Type = QueryResponseType.None } } }
            };
            var responseJson = JsonSerializer.Serialize(mockPipelineResponse, _jsonOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseJson, Encoding.UTF8, "application/json") };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage, req => capturedRequest = req);
            var client = new LibSqlClient("http://localhost", httpClient);

            // Act
            await client.QueryAsync("SELECT ?", new[] { argumentValue });

            // Assert
            Assert.That(capturedRequest, Is.Not.Null, "HTTP request was not captured.");
            Assert.That(capturedRequest.Content, Is.Not.Null, "HTTP request content is null.");
            
            var requestBody = await capturedRequest.Content.ReadAsStringAsync();
            var pipelineCall = JsonSerializer.Deserialize<PipelineCall>(requestBody, _jsonOptions);

            Assert.That(pipelineCall, Is.Not.Null, "Could not deserialize request body.");
            Assert.That(pipelineCall.Requests, Is.Not.Null.And.Count.EqualTo(1));
            Assert.That(pipelineCall.Requests[0].Statement, Is.Not.Null);
            Assert.That(pipelineCall.Requests[0].Statement.Args, Is.Not.Null.And.Count.EqualTo(1));

            var actualArg = pipelineCall.Requests[0].Statement.Args[0];
            Assert.That(actualArg.Type, Is.EqualTo(expectedLibSqlValue.Type), "Argument type mismatch.");
            
            // For float/real, direct value comparison might be tricky due to precision if numbers are deserialized.
            // The LibSqlValueConverter should handle this by storing them as strings internally if they were originally numbers.
            // However, our expected LibSqlValue for float/real has Value as string representation for consistent comparison.
            if (expectedLibSqlValue.Type == LibSqlValueType.Real)
            {
                // The value from JSON deserialization for "float" might be a JsonElement of type Number.
                // The LibSqlValueConverter should have converted it to a string.
                 Assert.That(actualArg.Value.ToString(), Is.EqualTo(expectedLibSqlValue.Value.ToString()), "Argument real value mismatch.");
            }
            else
            {
                Assert.That(actualArg.Value, Is.EqualTo(expectedLibSqlValue.Value), "Argument value mismatch.");
            }
        }
        
        [Test]
        public async Task QueryAsync_WithNullArguments_SendsNullArgsInStatement()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            var mockPipelineResponse = new PipelineResponse 
            {
                Results = new List<PipelineResult> { new PipelineResult { Type = PipelineResultType.Ok, Response = new QueryResult { Type = QueryResponseType.None } } }
            };
            var responseJson = JsonSerializer.Serialize(mockPipelineResponse, _jsonOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseJson, Encoding.UTF8, "application/json") };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage, req => capturedRequest = req);
            var client = new LibSqlClient("http://localhost", httpClient);

            // Act
            await client.QueryAsync("SELECT 1", null);

            // Assert
            Assert.That(capturedRequest, Is.Not.Null, "HTTP request was not captured.");
            var requestBody = await capturedRequest.Content.ReadAsStringAsync();
            var pipelineCall = JsonSerializer.Deserialize<PipelineCall>(requestBody, _jsonOptions);
            
            Assert.That(pipelineCall, Is.Not.Null);
            Assert.That(pipelineCall.Requests, Is.Not.Null.And.Count.EqualTo(1));
            Assert.That(pipelineCall.Requests[0].Statement, Is.Not.Null);
            Assert.That(pipelineCall.Requests[0].Statement.Args, Is.Null.Or.Empty, "Args should be null or empty when argument array is null.");
        }

        [Test]
        public async Task QueryAsync_WithEmptyArgumentList_SendsEmptyArgsInStatement()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            var mockPipelineResponse = new PipelineResponse 
            {
                Results = new List<PipelineResult> { new PipelineResult { Type = PipelineResultType.Ok, Response = new QueryResult { Type = QueryResponseType.None } } }
            };
            var responseJson = JsonSerializer.Serialize(mockPipelineResponse, _jsonOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseJson, Encoding.UTF8, "application/json") };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage, req => capturedRequest = req);
            var client = new LibSqlClient("http://localhost", httpClient);

            // Act
            await client.QueryAsync("SELECT 1", new object[] { });

            // Assert
            Assert.That(capturedRequest, Is.Not.Null, "HTTP request was not captured.");
            var requestBody = await capturedRequest.Content.ReadAsStringAsync();
            var pipelineCall = JsonSerializer.Deserialize<PipelineCall>(requestBody, _jsonOptions);

            Assert.That(pipelineCall, Is.Not.Null);
            Assert.That(pipelineCall.Requests, Is.Not.Null.And.Count.EqualTo(1));
            Assert.That(pipelineCall.Requests[0].Statement, Is.Not.Null);
            Assert.That(pipelineCall.Requests[0].Statement.Args, Is.Null.Or.Empty, "Args should be null or empty when argument array is empty.");
        }

        // Parameterized Query Execution Test
        [Test]
        public async Task QueryAsync_ParameterizedQuery_SendsCorrectSqlAndArgs()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            var mockPipelineResponse = new PipelineResponse 
            {
                Results = new List<PipelineResult> { new PipelineResult { Type = PipelineResultType.Ok, Response = new QueryResult { Type = QueryResponseType.None } } }
            };
            var responseJson = JsonSerializer.Serialize(mockPipelineResponse, _jsonOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseJson, Encoding.UTF8, "application/json") };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage, req => capturedRequest = req);
            var client = new LibSqlClient("http://localhost", httpClient);
            
            var query = "SELECT ? WHERE id = ?";
            var args = new object[] { "test_value", 101 };

            // Act
            await client.QueryAsync(query, args);

            // Assert
            Assert.That(capturedRequest, Is.Not.Null, "HTTP request was not captured.");
            var requestBody = await capturedRequest.Content.ReadAsStringAsync();
            var pipelineCall = JsonSerializer.Deserialize<PipelineCall>(requestBody, _jsonOptions);

            Assert.That(pipelineCall, Is.Not.Null);
            Assert.That(pipelineCall.Requests, Is.Not.Null.And.Count.EqualTo(1));
            var statement = pipelineCall.Requests[0].Statement;
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement.Sql, Is.EqualTo(query));
            Assert.That(statement.Args, Is.Not.Null.And.Count.EqualTo(2));
            Assert.That(statement.Args[0].Type, Is.EqualTo(LibSqlValueType.Text));
            Assert.That(statement.Args[0].Value, Is.EqualTo("test_value"));
            Assert.That(statement.Args[1].Type, Is.EqualTo(LibSqlValueType.Integer));
            Assert.That(statement.Args[1].Value, Is.EqualTo("101"));
        }

        // QueryMultipleAsync Tests
        [Test]
        public async Task QueryMultipleAsync_SuccessfulExecution_ReturnsMultipleResults()
        {
            // Arrange
            var queries = new List<SqlQuery>
            {
                new SqlQuery("SELECT 1 AS Value"),
                new SqlQuery("SELECT 'hello' AS Message")
            };

            var expectedBaton = "next_baton_value_123";
            var pipelineResponse = new PipelineResponse
            {
                Baton = expectedBaton,
                Results = new List<PipelineResult>
                {
                    new PipelineResult
                    {
                        Type = PipelineResultType.Ok,
                        Response = new QueryResult
                        {
                            Type = QueryResponseType.Row,
                            Cols = new List<QueryCol> { new QueryCol { Name = "Value" } },
                            Rows = new List<List<LibSqlValue>> { new List<LibSqlValue> { new LibSqlValue { Type = LibSqlValueType.Integer, Value = "1" } } }
                        }
                    },
                    new PipelineResult
                    {
                        Type = PipelineResultType.Ok,
                        Response = new QueryResult
                        {
                            Type = QueryResponseType.Row,
                            Cols = new List<QueryCol> { new QueryCol { Name = "Message" } },
                            Rows = new List<List<LibSqlValue>> { new List<LibSqlValue> { new LibSqlValue { Type = LibSqlValueType.Text, Value = "hello" } } }
                        }
                    }
                }
            };
            var responseJson = JsonSerializer.Serialize(pipelineResponse, _jsonOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage);
            var client = new LibSqlClient("http://localhost", httpClient);
            client.Baton = "initial_baton_test"; // To check if it gets updated

            // Act
            var result = await client.QueryMultipleAsync(queries);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Baton, Is.EqualTo(expectedBaton));
            Assert.That(client.Baton, Is.EqualTo(expectedBaton)); // Client's baton should be updated

            Assert.That(result.Results, Is.Not.Null);
            Assert.That(result.Results.Count, Is.EqualTo(2));

            // Assert first result
            var result1 = result.Results[0];
            Assert.That(result1.Type, Is.EqualTo(PipelineResultType.Ok));
            Assert.That(result1.Error, Is.Null);
            Assert.That(result1.Response, Is.Not.Null);
            Assert.That(result1.Response.Type, Is.EqualTo(QueryResponseType.Row));
            Assert.That(result1.Response.Cols, Is.Not.Null.And.Count.EqualTo(1));
            Assert.That(result1.Response.Cols[0].Name, Is.EqualTo("Value"));
            Assert.That(result1.Response.Rows, Is.Not.Null.And.Count.EqualTo(1));
            Assert.That(result1.Response.Rows[0][0].Type, Is.EqualTo(LibSqlValueType.Integer));
            Assert.That(result1.Response.Rows[0][0].Value, Is.EqualTo("1"));

            // Assert second result
            var result2 = result.Results[1];
            Assert.That(result2.Type, Is.EqualTo(PipelineResultType.Ok));
            Assert.That(result2.Error, Is.Null);
            Assert.That(result2.Response, Is.Not.Null);
            Assert.That(result2.Response.Type, Is.EqualTo(QueryResponseType.Row));
            Assert.That(result2.Response.Cols, Is.Not.Null.And.Count.EqualTo(1));
            Assert.That(result2.Response.Cols[0].Name, Is.EqualTo("Message"));
            Assert.That(result2.Response.Rows, Is.Not.Null.And.Count.EqualTo(1));
            Assert.That(result2.Response.Rows[0][0].Type, Is.EqualTo(LibSqlValueType.Text));
            Assert.That(result2.Response.Rows[0][0].Value, Is.EqualTo("hello"));
        }

        [Test]
        public void QueryMultipleAsync_ErrorInOneQuery_ThrowsLibSqlException()
        {
            // Arrange
            var queries = new List<SqlQuery>
            {
                new SqlQuery("SELECT 1"),
                new SqlQuery("SELECT INVALID_SYNTAX")
            };

            var expectedErrorMessage = "Syntax error near INVALID_SYNTAX";
            var expectedBaton = "baton_after_error";
            var pipelineResponse = new PipelineResponse
            {
                Baton = expectedBaton,
                Results = new List<PipelineResult>
                {
                    new PipelineResult // First query is successful
                    {
                        Type = PipelineResultType.Ok,
                        Response = new QueryResult
                        {
                            Type = QueryResponseType.Row,
                            Cols = new List<QueryCol> { new QueryCol { Name = "1" } },
                            Rows = new List<List<LibSqlValue>> { new List<LibSqlValue> { new LibSqlValue { Type = LibSqlValueType.Integer, Value = "1" } } }
                        }
                    },
                    new PipelineResult // Second query has an error
                    {
                        Type = PipelineResultType.Error,
                        Error = new PipelineResultError { Message = expectedErrorMessage }
                    }
                }
            };
            var responseJson = JsonSerializer.Serialize(pipelineResponse, _jsonOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) // HTTP call is OK
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage);
            var client = new LibSqlClient("http://localhost", httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<LibSqlException>(async () => await client.QueryMultipleAsync(queries));
            Assert.That(ex.Message, Does.Contain(expectedErrorMessage));
            Assert.That(ex.Message, Does.Contain("SELECT INVALID_SYNTAX")); // Check if failing query is in message
            
            // Verify client's baton is updated even if there was an error in results
            Assert.That(client.Baton, Is.EqualTo(expectedBaton));
        }

        [Test]
        public async Task QueryMultipleAsync_RequestIncludesClientBaton()
        {
            // Arrange
            var initialBaton = "initial_baton_456";
            var queries = new List<SqlQuery> { new SqlQuery("SELECT 1") };
            HttpRequestMessage? capturedRequest = null;

            var mockPipelineResponse = new PipelineResponse // Minimal success response
            {
                Baton = "new_baton_789",
                Results = new List<PipelineResult>
                {
                    new PipelineResult
                    {
                        Type = PipelineResultType.Ok,
                        Response = new QueryResult { Type = QueryResponseType.None }
                    }
                }
            };
            var responseJson = JsonSerializer.Serialize(mockPipelineResponse, _jsonOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage, req => capturedRequest = req);
            var client = new LibSqlClient("http://localhost", httpClient)
            {
                Baton = initialBaton
            };

            // Act
            await client.QueryMultipleAsync(queries);

            // Assert
            Assert.That(capturedRequest, Is.Not.Null, "HTTP request was not captured.");
            Assert.That(capturedRequest.Content, Is.Not.Null, "HTTP request content is null.");

            var requestBody = await capturedRequest.Content.ReadAsStringAsync();
            var pipelineCall = JsonSerializer.Deserialize<PipelineCall>(requestBody, _jsonOptions);

            Assert.That(pipelineCall, Is.Not.Null, "Could not deserialize request body to PipelineCall.");
            Assert.That(pipelineCall.Baton, Is.EqualTo(initialBaton), "PipelineCall.Baton does not match client's initial baton.");
            Assert.That(client.Baton, Is.EqualTo("new_baton_789"), "Client baton was not updated from response.");
        }
    }
}
