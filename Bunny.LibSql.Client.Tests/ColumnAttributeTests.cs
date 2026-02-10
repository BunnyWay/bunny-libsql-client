using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bunny.LibSql.Client.LINQ;
using Bunny.LibSql.Client.Migrations;
using Bunny.LibSql.Client.Migrations.InternalModels;
using Bunny.LibSql.Client.SQL;

namespace Bunny.LibSql.Client.Tests;

public class ColumnAttributeTests
{
    [Table("users")]
    private class User
    {
        [Key]
        [Column("user_id")]
        public int Id { get; set; }

        [Column("user_name")]
        public string UserName { get; set; } = string.Empty;
    }


    [Test]
    public void LinqToSqliteVisitor_Uses_ColumnAttribute_InWhere()
    {
        var data = new List<User>().AsQueryable();
        var query = data.Where(x => x.UserName == "Alice");

        var visitor = new LinqToSqliteVisitor([]);
        var (Sql, Parameters) = visitor.Translate(query.Expression);

        Assert.Multiple(() =>
        {
            Assert.That(Sql, Is.EqualTo("SELECT users.* FROM users WHERE (user_name = ?);"));
            Assert.That(Parameters.ToArray(), Is.EqualTo(new object[] { "Alice" }));
        });

    }

    [Test]
    public void SqlQueryBuilder_Uses_ColumnAttribute_InInsertAndUpdate()
    {
        var user = new User
        {
            Id = 1,
            UserName = "Bob"
        };

        var insert = SqlQueryBuilder.BuildInsertQuery("users", user);
        var update = SqlQueryBuilder.BuildUpdateQuery("users", user, "user_id", user.Id);

        Assert.Multiple(() =>
        {
            Assert.That(insert.SqlCommand, Is.EqualTo("INSERT INTO users (user_name) VALUES (?)"));
            Assert.That(update.SqlCommand, Does.Contain("user_name = ?"));
            Assert.That(update.SqlCommand, Does.Contain("UPDATE users SET"));
        });
    }

    [Test]
    public void TableSynchronizer_Uses_ColumnAttribute_WhenMatchingExistingColumns()
    {
        var existingColumns = new[]
        {
            new SqliteTableInfo
            {
                name = "user_id",
                type = "INTEGER",
                notnull = true,
                pk = 1
            },
            new SqliteTableInfo
            {
                name = "user_name",
                type = "TEXT",
                notnull = false,
                pk = 0
            }
        };

        var sql = TableSynchronizer.GenerateSqlCommands(typeof(User), existingColumns, []);

        Assert.That(sql, Is.Empty);
    }

    [Test]
    public void TableSynchronizer_Renames_Column_When_Attribute_Differs()
    {
        var existingColumns = new[]
        {
            new SqliteTableInfo
            {
                name = "Id",
                type = "INTEGER",
                notnull = true,
                pk = 1
            },
            new SqliteTableInfo
            {
                name = "UserName",
                type = "TEXT",
                notnull = false,
                pk = 0
            }
        };

        var sql = TableSynchronizer.GenerateSqlCommands(typeof(User), existingColumns, []);

        Assert.That(sql, Is.EquivalentTo(new[]
        {
            "ALTER TABLE users RENAME COLUMN Id TO user_id;",
            "ALTER TABLE users RENAME COLUMN UserName TO user_name;"
        }));
    }

    [Test]
    public void TableSynchronizer_Rebuilds_With_Renamed_Source_Columns()
    {
        var existingColumns = new[]
        {
            new SqliteTableInfo
            {
                name = "Id",
                type = "INTEGER",
                notnull = true,
                pk = 1
            },
            new SqliteTableInfo
            {
                name = "UserName",
                type = "INTEGER",
                notnull = false,
                pk = 0
            }
        };

        var sql = TableSynchronizer.GenerateSqlCommands(typeof(User), existingColumns, []);
        var insert = sql.First(s => s.StartsWith("INSERT INTO users_new", StringComparison.OrdinalIgnoreCase));

        Assert.Multiple(() =>
        {
            Assert.That(insert, Does.Contain("Id AS user_id"));
            Assert.That(insert, Does.Contain("UserName AS user_name"));
        });
    }

}
