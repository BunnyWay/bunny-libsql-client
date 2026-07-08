
using Bunny.LibSql.Client.Migrations;
using Bunny.LibSql.Client.Tests.Models;

namespace Bunny.LibSql.Client.Tests;

public class TableSyncronizerTests
{
    [Test]
    public void GenerateMovieTable()
    {
        var expected = new[]
        {
            "CREATE TABLE IF NOT EXISTS movies (id INTEGER PRIMARY KEY AUTOINCREMENT, title TEXT, year INTEGER NOT NULL, full_emb F32_BLOB(4));",
            "CREATE INDEX IF NOT EXISTS idx_movies_title ON movies(title);"
        };

        var tableMemberType = typeof(Movie);
        var actual = TableSynchronizer.GenerateSqlCommands(tableMemberType, [], []);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GeneratePersonTable()
    {
        var expected = new[]
        {
            "CREATE TABLE IF NOT EXISTS Person (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, lastName TEXT, age REAL NOT NULL, code TEXT, date_joined INTEGER NOT NULL, age_nullable INTEGER);",
            "CREATE INDEX IF NOT EXISTS idx_Person_name ON Person(name);"
        };

        var tableMemberType = typeof(Person);
        var actual = TableSynchronizer.GenerateSqlCommands(tableMemberType, [], []);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GeneratePersonToolTable()
    {
        var expected = new[]
        {
            "CREATE TABLE IF NOT EXISTS PersonTool (id INTEGER PRIMARY KEY AUTOINCREMENT, person_id INTEGER NOT NULL, tool_id INTEGER NOT NULL);"
        };

        var tableMemberType = typeof(PersonTool);
        var actual = TableSynchronizer.GenerateSqlCommands(tableMemberType, [], []);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GenerateProductTable()
    {
        var expected = new[]
        {
            "CREATE TABLE IF NOT EXISTS Product (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, person_id TEXT);",
            "CREATE INDEX IF NOT EXISTS idx_Product_name ON Product(name);"
        };

        var tableMemberType = typeof(Product);
        var actual = TableSynchronizer.GenerateSqlCommands(tableMemberType, [], []);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GenerateToolTable()
    {
        var expected = new[]
        {
            "CREATE TABLE IF NOT EXISTS Tool (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT);"
        };

        var tableMemberType = typeof(Tool);
        var actual = TableSynchronizer.GenerateSqlCommands(tableMemberType, [], []);


        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GenerateNoKeyTable()
    {
        var expected = new[]
        {
            "CREATE TABLE IF NOT EXISTS NoKey (description TEXT);"
        };

        var tableMemberType = typeof(NoKey);
        var actual = TableSynchronizer.GenerateSqlCommands(tableMemberType, [], []);


        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GenerateStringKeyTable()
    {
        var expected = new[]
        {
            "CREATE TABLE IF NOT EXISTS StringKey (id TEXT PRIMARY KEY, description TEXT);"
        };

        var tableMemberType = typeof(StringKey);
        var actual = TableSynchronizer.GenerateSqlCommands(tableMemberType, [], []);


        Assert.That(actual, Is.EqualTo(expected));
    }
}