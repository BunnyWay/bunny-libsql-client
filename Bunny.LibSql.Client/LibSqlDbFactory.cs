namespace Bunny.LibSql.Client;

public class LibSqlDbFactory<T>
{
    public string Path { get; set; }
    public string ApiKey { get; set; }
    
    public LibSqlDbFactory(string path, string apiKey)
    {
        Path = path;
        ApiKey = apiKey;
    }
    
    public T CreateDbContext()
    {
        var client = new LibSqlClient(Path, ApiKey);
        // activate an instance with reflection
        var instance = (T)Activator.CreateInstance(typeof(T), client);
        if (instance == null)
            throw new InvalidOperationException($"Could not create an instance of {typeof(T).Name}");
            
        return instance;
    }
}