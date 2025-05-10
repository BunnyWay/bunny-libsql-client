using System.Reflection;
using Bunny.LibSql.Client.Migrations;
using Bunny.LibSql.Client.Migrations.InternalModels;
using Bunny.LibSql.Client.SQL;

namespace Bunny.LibSql.Client;

public abstract class LibSqlDatabase
{
    public LibSqlClient Client { get; }

    protected LibSqlDatabase(LibSqlClient client)
    {
        Client = client;
        InitializeTables();
    }

    private void InitializeTables()
    {
        foreach (var table in GetAllTablesInCurrentDatabaseObject())
        {
            table.SetValue(this, Activator.CreateInstance(table.PropertyType, this));
        }
    }   
    
    public async Task ApplyMigrationsAsync()
    {
        var tables = GetAllTablesInCurrentDatabaseObject();
        foreach (var table in tables)
        {
            var tableName = table.GetType().GetLibSqlTableName();
            var indexes = await Client.QueryAsync<SqlMasterInfo>($"SELECT * FROM sqlite_master WHERE type= 'index' and tbl_name = '{tableName}'");
            var tableInfos = await Client.QueryAsync<TableInfo>($"pragma table_info({tableName})");

            var tableMemberType = table.PropertyType.GetGenericArguments()[0];
            var commands = TableSynchronizer.GenerateSqlCommands(tableMemberType, tableInfos, indexes);
            if (commands.Count > 0)
            {
                await Client.QueryMultipleAsync(commands.Select(e => new SqlQuery(e)).ToList());
            }
        }
    }

    private List<PropertyInfo> GetAllTablesInCurrentDatabaseObject()
    {
        return GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(LibSqlTable<>))
            .ToList();
    }
}