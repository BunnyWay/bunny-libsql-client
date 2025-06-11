using System.Reflection;
using Bunny.LibSql.Client.Migrations;
using Bunny.LibSql.Client.Migrations.InternalModels;
using Bunny.LibSql.Client.SQL;

namespace Bunny.LibSql.Client;

public abstract class LibSqlDbContext
{
    public LibSqlClient Client { get; }

    protected LibSqlDbContext(LibSqlClient client)
    {
        Client = client;
        InitializeTableObjects();
    }

    private void InitializeTableObjects()
    {
        foreach (var table in GetAllTablesInDatabase())
        {
            table.SetValue(this, Activator.CreateInstance(table.PropertyType, this));
        }
    }   
    
    public async Task ApplyMigrationsAsync()
    {
        var allCommands = new List<string>();
        
        var tables = GetAllTablesInDatabase();
        foreach (var table in tables)
        {
            var tableName = table.PropertyType.GetGenericArguments()[0].GetLibSqlTableName();
            
            var indexes = await Client.QueryAsync<SqliteMasterInfo>($"SELECT * FROM sqlite_master WHERE type= 'index' and tbl_name = '{tableName}'");
            var tableInfos = await Client.QueryAsync<SqliteTableInfo>($"pragma table_info({tableName})");

            var tableMemberType = table.PropertyType.GetGenericArguments()[0];
            var commands = TableSynchronizer.GenerateSqlCommands(tableMemberType, tableInfos, indexes);
            allCommands.AddRange(commands);
        }
        
        if (allCommands.Count > 0)
        {
            await Client.QueryMultipleAsync(allCommands.Select(e => new SqlQuery(e)).ToList());
        }
    }

    public PropertyInfo? GetDatabasePropertyForType(Type type)
    {
        var dbs = GetAllTablesInDatabase();
        var dbForType = dbs.Where(e => e.PropertyType.GetGenericArguments().First() == type).FirstOrDefault();
        return dbForType;
    }

    public List<PropertyInfo> GetAllTablesInDatabase()
    {
        return GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(LibSqlTable<>))
            .ToList();
    }
}