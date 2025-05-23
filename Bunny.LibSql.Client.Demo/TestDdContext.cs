using Bunny.LibSql.Client;
using Bunny.LibSql.Client.Demo;

namespace Bunny.LibSql.Client.Demo;

public class TestDdContext(LibSqlClient client) : LibSqlDbContext(client)
{
    public LibSqlTable<Person> People { get; set; }
    public LibSqlTable<Product> Products { get; set; }
    public LibSqlTable<Description> Descriptions { get; set; }
    public LibSqlTable<PersonTool> PersonTools { get; set; }
    public LibSqlTable<Tool> Tools { get; set; }
}