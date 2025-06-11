using System.Reflection;

namespace Bunny.LibSql.Client.Types;

public interface ILibSqlType
{
    public object? GetLibSqlJsonValue();
}