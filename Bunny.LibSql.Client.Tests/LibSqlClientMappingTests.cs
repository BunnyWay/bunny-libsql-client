using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public double Salary { get; set; }
        public bool IsActive { get; set; }
        public byte[]? Data { get; set; }
        // DateTime can be added if/when supported by the client's type mapping
        // public DateTime CreatedAt { get; set; } 
    }

    [TestFixture]
    public class LibSqlClientMappingTests
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters = { new LibSqlValue.LibSqlValueConverter() }
        };

        private LibSqlClient CreateClientWithMockResponse(PipelineResponse pipelineResponse)
        {
            var responseJson = JsonSerializer.Serialize(pipelineResponse, _jsonOptions);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage);
            return new LibSqlClient("http://localhost", httpClient);
        }

        // QueryAsync<T> Tests
        [Test]
        public async Task QueryAsync_Generic_SuccessfulMapping_ReturnsListOfEntities()
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
                            Cols = new List<QueryCol> 
                            { 
                                new QueryCol { Name = "Id" }, 
                                new QueryCol { Name = "Name" }, 
                                new QueryCol { Name = "Salary" }, 
                                new QueryCol { Name = "IsActive" },
                                new QueryCol { Name = "Data" }
                            },
                            Rows = new List<List<LibSqlValue>>
                            {
                                new List<LibSqlValue> 
                                { 
                                    new LibSqlValue { Type = LibSqlValueType.Integer, Value = "1" }, 
                                    new LibSqlValue { Type = LibSqlValueType.Text, Value = "Alice" }, 
                                    new LibSqlValue { Type = LibSqlValueType.Real, Value = "5000.50" }, 
                                    new LibSqlValue { Type = LibSqlValueType.Integer, Value = "1" },
                                    new LibSqlValue { Type = LibSqlValueType.Blob, Value = "AQI=" } // base64 for [1, 2]
                                },
                                new List<LibSqlValue> 
                                { 
                                    new LibSqlValue { Type = LibSqlValueType.Integer, Value = "2" }, 
                                    new LibSqlValue { Type = LibSqlValueType.Null }, 
                                    new LibSqlValue { Type = LibSqlValueType.Real, Value = "6000.75" }, 
                                    new LibSqlValue { Type = LibSqlValueType.Integer, Value = "0" },
                                    new LibSqlValue { Type = LibSqlValueType.Null }
                                }
                            }
                        }
                    }
                }
            };
            var client = CreateClientWithMockResponse(pipelineResponse);

            // Act
            var result = await client.QueryAsync<TestEntity>("SELECT * FROM test_entities");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));

            var entity1 = result[0];
            Assert.That(entity1.Id, Is.EqualTo(1));
            Assert.That(entity1.Name, Is.EqualTo("Alice"));
            Assert.That(entity1.Salary, Is.EqualTo(5000.50d));
            Assert.That(entity1.IsActive, Is.True);
            Assert.That(entity1.Data, Is.EqualTo(new byte[] { 1, 2 }));

            var entity2 = result[1];
            Assert.That(entity2.Id, Is.EqualTo(2));
            Assert.That(entity2.Name, Is.Null);
            Assert.That(entity2.Salary, Is.EqualTo(6000.75d));
            Assert.That(entity2.IsActive, Is.False);
            Assert.That(entity2.Data, Is.Null);
        }

        [Test]
        public async Task QueryAsync_Generic_ColumnNameCaseMismatch_MapsCorrectly()
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
                            Cols = new List<QueryCol> 
                            { 
                                new QueryCol { Name = "id" }, 
                                new QueryCol { Name = "name" }, 
                                new QueryCol { Name = "salary" }, 
                                new QueryCol { Name = "isactive" } 
                            },
                            Rows = new List<List<LibSqlValue>>
                            {
                                new List<LibSqlValue> 
                                { 
                                    new LibSqlValue { Type = LibSqlValueType.Integer, Value = "1" }, 
                                    new LibSqlValue { Type = LibSqlValueType.Text, Value = "Bob" }, 
                                    new LibSqlValue { Type = LibSqlValueType.Real, Value = "7000.00" }, 
                                    new LibSqlValue { Type = LibSqlValueType.Integer, Value = "1" }
                                }
                            }
                        }
                    }
                }
            };
            var client = CreateClientWithMockResponse(pipelineResponse);

            // Act
            var result = await client.QueryAsync<TestEntity>("SELECT * FROM test_entities");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            var entity = result.First();
            Assert.That(entity.Id, Is.EqualTo(1));
            Assert.That(entity.Name, Is.EqualTo("Bob"));
            Assert.That(entity.Salary, Is.EqualTo(7000.00d));
            Assert.That(entity.IsActive, Is.True);
        }

        [Test]
        public async Task QueryAsync_Generic_ExtraColumnsInResult_IgnoresExtraData()
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
                            Cols = new List<QueryCol> 
                            { 
                                new QueryCol { Name = "Id" }, 
                                new QueryCol { Name = "Name" }, 
                                new QueryCol { Name = "ExtraColumn" } 
                            },
                            Rows = new List<List<LibSqlValue>>
                            {
                                new List<LibSqlValue> 
                                { 
                                    new LibSqlValue { Type = LibSqlValueType.Integer, Value = "3" }, 
                                    new LibSqlValue { Type = LibSqlValueType.Text, Value = "Charlie" }, 
                                    new LibSqlValue { Type = LibSqlValueType.Text, Value = "ExtraValue" }
                                }
                            }
                        }
                    }
                }
            };
            var client = CreateClientWithMockResponse(pipelineResponse);

            // Act
            var result = await client.QueryAsync<TestEntity>("SELECT Id, Name, ExtraColumn FROM test_entities");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            var entity = result.First();
            Assert.That(entity.Id, Is.EqualTo(3));
            Assert.That(entity.Name, Is.EqualTo("Charlie"));
            // Salary, IsActive, Data should be default
            Assert.That(entity.Salary, Is.EqualTo(default(double)));
            Assert.That(entity.IsActive, Is.EqualTo(default(bool)));
            Assert.That(entity.Data, Is.Null);
        }

        [Test]
        public async Task QueryAsync_Generic_MissingColumnsInResult_SetsDefaultValues()
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
                            Cols = new List<QueryCol> 
                            { 
                                new QueryCol { Name = "Id" }, 
                                new QueryCol { Name = "Name" } 
                            },
                            Rows = new List<List<LibSqlValue>>
                            {
                                new List<LibSqlValue> 
                                { 
                                    new LibSqlValue { Type = LibSqlValueType.Integer, Value = "4" }, 
                                    new LibSqlValue { Type = LibSqlValueType.Text, Value = "David" }
                                }
                            }
                        }
                    }
                }
            };
            var client = CreateClientWithMockResponse(pipelineResponse);

            // Act
            var result = await client.QueryAsync<TestEntity>("SELECT Id, Name FROM test_entities");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            var entity = result.First();
            Assert.That(entity.Id, Is.EqualTo(4));
            Assert.That(entity.Name, Is.EqualTo("David"));
            Assert.That(entity.Salary, Is.EqualTo(default(double)));
            Assert.That(entity.IsActive, Is.EqualTo(default(bool)));
            Assert.That(entity.Data, Is.Null);
        }

        // QueryOneAsync<T> Tests
        [Test]
        public async Task QueryOneAsync_Generic_SingleRow_ReturnsSingleEntity()
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
                            Cols = new List<QueryCol> { new QueryCol { Name = "Id" }, new QueryCol { Name = "Name" } },
                            Rows = new List<List<LibSqlValue>>
                            {
                                new List<LibSqlValue> { new LibSqlValue { Type = LibSqlValueType.Integer, Value = "5" }, new LibSqlValue { Type = LibSqlValueType.Text, Value = "Eve" } }
                            }
                        }
                    }
                }
            };
            var client = CreateClientWithMockResponse(pipelineResponse);

            // Act
            var entity = await client.QueryOneAsync<TestEntity>("SELECT * FROM test_entities WHERE Id = 5");

            // Assert
            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.Id, Is.EqualTo(5));
            Assert.That(entity.Name, Is.EqualTo("Eve"));
        }

        [Test]
        public async Task QueryOneAsync_Generic_MultipleRows_ReturnsFirstEntity()
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
                            Cols = new List<QueryCol> { new QueryCol { Name = "Id" }, new QueryCol { Name = "Name" } },
                            Rows = new List<List<LibSqlValue>>
                            {
                                new List<LibSqlValue> { new LibSqlValue { Type = LibSqlValueType.Integer, Value = "6" }, new LibSqlValue { Type = LibSqlValueType.Text, Value = "Frank" } },
                                new List<LibSqlValue> { new LibSqlValue { Type = LibSqlValueType.Integer, Value = "7" }, new LibSqlValue { Type = LibSqlValueType.Text, Value = "Grace" } }
                            }
                        }
                    }
                }
            };
            var client = CreateClientWithMockResponse(pipelineResponse);

            // Act
            var entity = await client.QueryOneAsync<TestEntity>("SELECT * FROM test_entities");

            // Assert
            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.Id, Is.EqualTo(6));
            Assert.That(entity.Name, Is.EqualTo("Frank"));
        }

        [Test]
        public async Task QueryOneAsync_Generic_NoRows_ReturnsNull()
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
                            Cols = new List<QueryCol> { new QueryCol { Name = "Id" } }, // Cols can be present even if no rows
                            Rows = new List<List<LibSqlValue>>() // Empty rows
                        }
                    }
                }
            };
            var client = CreateClientWithMockResponse(pipelineResponse);

            // Act
            var entity = await client.QueryOneAsync<TestEntity>("SELECT * FROM test_entities WHERE Id = 999");

            // Assert
            Assert.That(entity, Is.Null);
        }
    }
}
