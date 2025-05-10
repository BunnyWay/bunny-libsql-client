using Bunny.LibSql.Client;
using Bunny.LibSql.Client.Demo;

namespace Bunny.LibSql.Client.Demo;

public class TestDd : LibSqlDatabase
{
    public TestDd(LibSqlClient client) : base(client)
    {
    }
    
    public TestDd(string dbUrl, string accessKey) : base(new LibSqlClient(dbUrl, accessKey))
    {
    }
    
    public LibSqlTable<Person> People { get; set; }
    public LibSqlTable<Product> Products { get; set; }
    public LibSqlTable<Description> Descriptions { get; set; }
}