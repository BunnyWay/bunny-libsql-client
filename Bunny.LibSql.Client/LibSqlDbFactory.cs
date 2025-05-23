namespace Bunny.LibSql.Client;

public class LibSqlDbFactory<T>
{
    public LibSqlClient Client { get; set; }
    public LibSqlDbFactory(string path, string apiKey)
    {
        Client = new LibSqlClient(path, apiKey);
    }
    
    public T CreateDbContext()
    {
        // activate an instance with reflection
        var instance = (T)Activator.CreateInstance(typeof(T), Client);
        if (instance == null)
            throw new InvalidOperationException($"Could not create an instance of {typeof(T).Name}");
            
        return instance;
    }
}