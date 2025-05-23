using NUnit.Framework;
using Moq;
using System.Linq;
using System.Reflection;
using Bunny.LibSql.Client; // For LibSqlDbContext, LibSqlClient, LibSqlTable
using Bunny.LibSql.Client.Attributes; // For IndexAttribute
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Bunny.LibSql.Client.HttpClientModels; // For PipelineResponse (or define dummy if not needed for QueryMultipleAsync mock)
using Bunny.LibSql.Client.SQL; // For SqlQuery

namespace Bunny.LibSql.Client.Tests
{
    // Simplified internal models for mocking purposes
    public class SqliteMasterInfo
    {
        public string? Type { get; set; }
        public string? Name { get; set; }
        public string? Tbl_Name { get; set; } // Keep underscore for direct mapping from query
        public int Rootpage { get; set; }
        public string? Sql { get; set; }
    }

    public class SqliteTableInfo
    {
        public int Cid { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; } // Data type
        public int NotNull { get; set; } // 0 or 1
        public string? Dflt_Value { get; set; } // Keep underscore
        public int Pk { get; set; } // 0 or 1 (primary key)
    }


    // Test Entities
    public class Product
    {
        public int Id { get; set; } // Primary key by convention
        public string? Name { get; set; }
        [Index]
        public string? Sku { get; set; }
        public decimal Price { get; set; }
    }

    public class Category // Kept from previous, though not directly used in migration tests
    {
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }

    // Test DbContext
    public class TestDbContext : LibSqlDbContext
    {
        public LibSqlTable<Product> Products { get; set; }
        public LibSqlTable<Category> Categories { get; set; } // Kept for consistency
        public string? NonTableProperty { get; set; }

        public TestDbContext(LibSqlClient client) : base(client) { }
    }

    [TestFixture]
    public class LibSqlDbContextTests
    {
        private Mock<LibSqlClient> _mockClient = null!; // Non-nullable is initialized in SetUp

        [SetUp]
        public void SetUp()
        {
            _mockClient = new Mock<LibSqlClient>("http://dummy.url");
        }

        // Test LibSqlDbContext Initialization (from previous step)
        [Test]
        public void DbContext_Initialization_InitializesTableProperties()
        {
            // Arrange
            var dbContext = new TestDbContext(_mockClient.Object);

            // Assert
            Assert.That(dbContext.Products, Is.Not.Null, "Products table should be initialized.");
            Assert.That(dbContext.Categories, Is.Not.Null, "Categories table should be initialized.");
            Assert.That(dbContext.Products.Db.Client, Is.SameAs(_mockClient.Object));
            Assert.That(dbContext.Categories.Db.Client, Is.SameAs(_mockClient.Object));
        }

        // Test GetAllTablesInDatabase Method (from previous step)
        [Test]
        public void GetAllTablesInDatabase_ReturnsCorrectTableProperties()
        {
            // Arrange
            var dbContext = new TestDbContext(_mockClient.Object);
            var tableProperties = dbContext.GetAllTablesInDatabase();
            Assert.That(tableProperties.Count, Is.EqualTo(2)); // Products and Categories
            Assert.That(tableProperties.Any(p => p.Name == "Products"), Is.True);
            Assert.That(tableProperties.Any(p => p.Name == "Categories"), Is.True);
        }

        // Test GetDatabasePropertyForType Method (from previous step)
        [Test]
        public void GetDatabasePropertyForType_ReturnsCorrectProperty()
        {
            var dbContext = new TestDbContext(_mockClient.Object);
            var productProperty = dbContext.GetDatabasePropertyForType(typeof(Product));
            Assert.That(productProperty?.Name, Is.EqualTo("Products"));
        }

        [Test]
        public void GetDatabasePropertyForType_UnknownType_ReturnsNull()
        {
            var dbContext = new TestDbContext(_mockClient.Object);
            var stringProperty = dbContext.GetDatabasePropertyForType(typeof(string));
            Assert.That(stringProperty, Is.Null);
        }

        // ApplyMigrationsAsync Tests
        [Test]
        public async Task ApplyMigrationsAsync_EmptyDatabase_CreatesTableAndIndexes()
        {
            // Arrange
            _mockClient.Setup(c => c.QueryAsync<SqliteTableInfo>(It.Is<string>(s => s.ToLower().StartsWith("pragma table_info('products')")), null, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<SqliteTableInfo>());

            _mockClient.Setup(c => c.QueryAsync<SqliteMasterInfo>(It.Is<string>(s => s.ToLower().Contains("tbl_name = 'products'")), null, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<SqliteMasterInfo>());
            
            // Mock for Categories table as well, assuming it's also new
             _mockClient.Setup(c => c.QueryAsync<SqliteTableInfo>(It.Is<string>(s => s.ToLower().StartsWith("pragma table_info('categories')")), null, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<SqliteTableInfo>());
             _mockClient.Setup(c => c.QueryAsync<SqliteMasterInfo>(It.Is<string>(s => s.ToLower().Contains("tbl_name = 'categories'")), null, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<SqliteMasterInfo>());


            List<SqlQuery>? executedCommands = null;
            var mockPipelineResponse = new PipelineResponse { Results = new List<PipelineResult>() }; // Dummy success
            _mockClient.Setup(c => c.QueryMultipleAsync(It.IsAny<List<SqlQuery>>(), It.IsAny<CancellationToken>()))
                       .Callback<List<SqlQuery>, CancellationToken>((cmds, ct) => executedCommands = cmds)
                       .ReturnsAsync(mockPipelineResponse);

            var dbContext = new TestDbContext(_mockClient.Object);

            // Act
            await dbContext.ApplyMigrationsAsync();

            // Assert
            Assert.That(executedCommands, Is.Not.Null, "QueryMultipleAsync should have been called.");
            Assert.That(executedCommands, Is.Not.Empty, "Commands should have been executed.");

            var createProductTableCmd = executedCommands!.FirstOrDefault(cmd => cmd.Sql.ToLower().Contains("create table \"products\""));
            Assert.That(createProductTableCmd, Is.Not.Null, "CREATE TABLE Product command not found.");
            // Note: Exact SQL for column types (TEXT, INTEGER, REAL) and constraints (PRIMARY KEY, NULL/NOT NULL)
            // depends on TableSynchronizer logic. These are common expectations.
            Assert.That(createProductTableCmd!.Sql, Does.Match(@"CREATE TABLE ""Products"" \(\s*""Id"" INTEGER PRIMARY KEY AUTOINCREMENT,\s*""Name"" TEXT,\s*""Sku"" TEXT,\s*""Price"" REAL\s*\)"), "Product table schema mismatch.");

            var createProductIndexCmd = executedCommands!.FirstOrDefault(cmd => cmd.Sql.ToLower().Contains("create index ix_products_sku on \"products\" (sku)"));
            Assert.That(createProductIndexCmd, Is.Not.Null, "CREATE INDEX IX_Product_Sku command not found.");
            
            // We should also see commands for the "Categories" table if it's part of the context
            var createCategoryTableCmd = executedCommands!.FirstOrDefault(cmd => cmd.Sql.ToLower().Contains("create table \"categories\""));
            Assert.That(createCategoryTableCmd, Is.Not.Null, "CREATE TABLE Categories command not found.");
        }

        [Test]
        public async Task ApplyMigrationsAsync_TableExists_AddsMissingColumnAndIndex()
        {
            // Arrange
            var existingProductTableInfo = new List<SqliteTableInfo>
            {
                new SqliteTableInfo { Name = "Id", Type = "INTEGER", Pk = 1 },
                new SqliteTableInfo { Name = "Name", Type = "TEXT" }
                // Sku and Price are missing
            };
            _mockClient.Setup(c => c.QueryAsync<SqliteTableInfo>(It.Is<string>(s => s.ToLower().StartsWith("pragma table_info('products')")), null, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(existingProductTableInfo);
            _mockClient.Setup(c => c.QueryAsync<SqliteMasterInfo>(It.Is<string>(s => s.ToLower().Contains("tbl_name = 'products'")), null, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<SqliteMasterInfo>()); // No Sku index

            // Assume Categories table exists and is up-to-date for simplicity of this test
            var completeCategoryTableInfo = new List<SqliteTableInfo> {
                new SqliteTableInfo { Name = "CategoryId", Type = "INTEGER", Pk = 1 },
                new SqliteTableInfo { Name = "CategoryName", Type = "TEXT" }
            };
            _mockClient.Setup(c => c.QueryAsync<SqliteTableInfo>(It.Is<string>(s => s.ToLower().StartsWith("pragma table_info('categories')")), null, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(completeCategoryTableInfo);
            _mockClient.Setup(c => c.QueryAsync<SqliteMasterInfo>(It.Is<string>(s => s.ToLower().Contains("tbl_name = 'categories'")), null, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<SqliteMasterInfo>()); // No indexes on Category for this test


            List<SqlQuery>? executedCommands = null;
            var mockPipelineResponse = new PipelineResponse { Results = new List<PipelineResult>() };
            _mockClient.Setup(c => c.QueryMultipleAsync(It.IsAny<List<SqlQuery>>(), It.IsAny<CancellationToken>()))
                       .Callback<List<SqlQuery>, CancellationToken>((cmds, ct) => executedCommands = cmds)
                       .ReturnsAsync(mockPipelineResponse);

            var dbContext = new TestDbContext(_mockClient.Object);

            // Act
            await dbContext.ApplyMigrationsAsync();

            // Assert
            Assert.That(executedCommands, Is.Not.Null);
            Assert.That(executedCommands, Is.Not.Empty);
            
            Assert.That(executedCommands!.Any(cmd => cmd.Sql.ToLower().Contains("create table \"products\"")), Is.False, "Should not try to recreate Product table.");
            
            var addSkuColumnCmd = executedCommands!.FirstOrDefault(cmd => cmd.Sql.ToLower().Contains("alter table \"products\" add column \"sku\" text"));
            Assert.That(addSkuColumnCmd, Is.Not.Null, "ALTER TABLE Product ADD COLUMN Sku command not found.");

            var addPriceColumnCmd = executedCommands!.FirstOrDefault(cmd => cmd.Sql.ToLower().Contains("alter table \"products\" add column \"price\" real"));
            Assert.That(addPriceColumnCmd, Is.Not.Null, "ALTER TABLE Product ADD COLUMN Price command not found.");

            var createIndexCmd = executedCommands!.FirstOrDefault(cmd => cmd.Sql.ToLower().Contains("create index ix_products_sku on \"products\" (sku)"));
            Assert.That(createIndexCmd, Is.Not.Null, "CREATE INDEX IX_Product_Sku command not found.");
        }

        [Test]
        public async Task ApplyMigrationsAsync_SchemaMatches_NoCommandsExecuted()
        {
            // Arrange
            var completeProductTableInfo = new List<SqliteTableInfo> {
                new SqliteTableInfo { Name = "Id", Type = "INTEGER", Pk = 1 },
                new SqliteTableInfo { Name = "Name", Type = "TEXT" },
                new SqliteTableInfo { Name = "Sku", Type = "TEXT" },
                new SqliteTableInfo { Name = "Price", Type = "REAL" }
            };
            _mockClient.Setup(c => c.QueryAsync<SqliteTableInfo>(It.Is<string>(s => s.ToLower().StartsWith("pragma table_info('products')")), null, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(completeProductTableInfo);

            var existingProductIndexInfo = new List<SqliteMasterInfo> {
                new SqliteMasterInfo { Name = "IX_Products_Sku", Tbl_Name = "Products", Sql = "CREATE INDEX IX_Products_Sku ON Products (Sku)" } // Name must match convention
            };
            _mockClient.Setup(c => c.QueryAsync<SqliteMasterInfo>(It.Is<string>(s => s.ToLower().Contains("tbl_name = 'products'")), null, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(existingProductIndexInfo);

            // Assume Categories table also exists and is up-to-date
             var completeCategoryTableInfo = new List<SqliteTableInfo> {
                new SqliteTableInfo { Name = "CategoryId", Type = "INTEGER", Pk = 1 },
                new SqliteTableInfo { Name = "CategoryName", Type = "TEXT" }
            };
            _mockClient.Setup(c => c.QueryAsync<SqliteTableInfo>(It.Is<string>(s => s.ToLower().StartsWith("pragma table_info('categories')")), null, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(completeCategoryTableInfo);
            _mockClient.Setup(c => c.QueryAsync<SqliteMasterInfo>(It.Is<string>(s => s.ToLower().Contains("tbl_name = 'categories'")), null, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<SqliteMasterInfo>());


            List<SqlQuery>? executedCommands = null;
            var mockPipelineResponse = new PipelineResponse { Results = new List<PipelineResult>() };
            _mockClient.Setup(c => c.QueryMultipleAsync(It.IsAny<List<SqlQuery>>(), It.IsAny<CancellationToken>()))
                       .Callback<List<SqlQuery>, CancellationToken>((cmds, ct) => executedCommands = cmds)
                       .ReturnsAsync(mockPipelineResponse);

            var dbContext = new TestDbContext(_mockClient.Object);

            // Act
            await dbContext.ApplyMigrationsAsync();

            // Assert
            // If ApplyMigrationsAsync is smart enough to not call QueryMultipleAsync when no commands,
            // executedCommands might remain null. Otherwise, it's an empty list.
            Assert.That(executedCommands == null || !executedCommands.Any(), Is.True, "No commands should be executed if schema matches.");
        }
    }
}
