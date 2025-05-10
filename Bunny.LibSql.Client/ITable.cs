using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Bunny.LibSql.Client.LINQ;

namespace Bunny.LibSql.Client;

public interface ITable
{ 
    string GetName();
}