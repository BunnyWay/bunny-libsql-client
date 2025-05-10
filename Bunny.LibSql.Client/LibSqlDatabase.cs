using System.Reflection;
using Bunny.LibSql.Client.LINQ;
using Bunny.LibSql.Client.Migrations;
using Bunny.LibSql.Client.Migrations.InternalModels;

namespace Bunny.LibSql.Client;

public abstract class LibSqlDatabase
{
    public LibSqlClient Client { get; set; }
    
    public LibSqlDatabase(LibSqlClient client)
    {
        Client = client;
        InitializeTables();
    }

    private void InitializeTables()
    {
        var tables = GetAllTables();
        foreach (var table in tables)
        {
            table.SetValue(this, Activator.CreateInstance(table.PropertyType, this));
        }
    }   
    
    public async Task ApplyMigrationsAsync()
    {
        var tables = GetAllTables();
        foreach (var table in tables)
        {
            var tableValue = (ITable)table.GetValue(this);
            
            var indexes = await Client.QueryAsync<SqlMasterInfo>($"SELECT * FROM sqlite_master WHERE type= 'index' and tbl_name = '{tableValue.GetName()}'");
            var tableInfos = await Client.QueryAsync<TableInfo>($"pragma table_info({tableValue.GetName()})");

            // Get T type of the table type
            var tType = table.PropertyType.GetGenericArguments()[0];
            
            var commands = TableSynchronizer.GenerateSqlCommands(tType, tableInfos, indexes);
            if (commands.Count > 0)
            {
                await Client.QueryMultipleAsync(commands.Select(e => new SqlQuery(e)).ToList());
            }
        }
    }
    
    public List<PropertyInfo> GetAllTables()
    {
        return this.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType
                        && p.PropertyType.GetGenericTypeDefinition() == typeof(LibSqlTable<>))
            .ToList();
    }
}