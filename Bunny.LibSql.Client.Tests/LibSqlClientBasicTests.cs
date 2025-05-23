using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bunny.LibSql.Client; // Assuming LibSqlClient is in this namespace
using Bunny.LibSql.Client.Tests; // For HttpClientMockHelper

namespace Bunny.LibSql.Client.Tests
{
    [TestFixture]
    public class LibSqlClientBasicTests
    {
        // Constructor Tests
        [Test]
        [TestCase("http://localhost:8080", "http://localhost:8080/")]
        [TestCase("https://localhost:8080", "https://localhost:8080/")]
        [TestCase("libsql://localhost:8080", "https://localhost:8080/")] // Should be converted
        public void Constructor_WithValidBaseUrl_InitializesCorrectly(string baseUrl, string expectedBaseAddress)
        {
            // Arrange & Act
            var client = new LibSqlClient(baseUrl);

            // Assert
            Assert.That(client._client.BaseAddress, Is.EqualTo(new Uri(expectedBaseAddress)));
        }

        [Test]
        [TestCase("")]
        [TestCase("ftp://invalid-scheme")]
        [TestCase("/path/to/db")]
        public void Constructor_WithInvalidBaseUrl_ThrowsArgumentException(string baseUrl)
        {
            // Assert
            Assert.Throws<ArgumentException>(() => new LibSqlClient(baseUrl));
        }

        // GetDatabaseVersionAsync Tests
        [Test]
        public async Task GetDatabaseVersionAsync_SuccessfulApiCall_ReturnsVersionString()
        {
            // Arrange
            var expectedVersion = "libsql-server-dev";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"\"{expectedVersion}\"", Encoding.UTF8, "application/json")
            };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage);
            var client = new LibSqlClient("http://localhost", httpClient);

            // Act
            var version = await client.GetDatabaseVersionAsync();

            // Assert
            Assert.That(version, Is.EqualTo(expectedVersion));
        }

        [Test]
        public void GetDatabaseVersionAsync_FailedApiCall_ThrowsHttpRequestException()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage);
            var client = new LibSqlClient("http://localhost", httpClient);

            // Assert
            Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetDatabaseVersionAsync());
        }

        // DumpDatabaseToStreamAsync Tests
        [Test]
        public async Task DumpDatabaseToStreamAsync_SuccessfulApiCall_WritesToStream()
        {
            // Arrange
            var expectedDumpContent = "CREATE TABLE test (id INT);";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(expectedDumpContent)
            };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage);
            var client = new LibSqlClient("http://localhost", httpClient);
            using var memoryStream = new MemoryStream();

            // Act
            await client.DumpDatabaseToStreamAsync(memoryStream);

            // Assert
            memoryStream.Position = 0;
            using var reader = new StreamReader(memoryStream, Encoding.UTF8);
            var actualDumpContent = await reader.ReadToEndAsync();
            Assert.That(actualDumpContent, Is.EqualTo(expectedDumpContent));
        }

        [Test]
        public void DumpDatabaseToStreamAsync_FailedApiCall_ThrowsLibSqlException()
        {
            // Arrange
            var errorMessage = "Error dumping database";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(errorMessage)
            };
            var httpClient = HttpClientMockHelper.CreateMockedHttpClient(responseMessage);
            var client = new LibSqlClient("http://localhost", httpClient);
            using var memoryStream = new MemoryStream();

            // Assert
            var ex = Assert.ThrowsAsync<LibSqlException>(async () => await client.DumpDatabaseToStreamAsync(memoryStream));
            Assert.That(ex.Message, Does.Contain(errorMessage));
        }
    }
}
