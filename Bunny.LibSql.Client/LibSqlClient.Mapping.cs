using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json.Enums;
using Bunny.LibSql.Client.LINQ;
using Bunny.LibSql.Client.SQL;

namespace Bunny.LibSql.Client
{
    public partial class LibSqlClient
    {
        private string CreatePipelinecallAsJson(List<SqlQuery> queries)
        {
            var call = new PipelineCall();
            call.Baton = Baton;
            foreach (var query in queries)   
            {
                call.Requests.Add(new PipelineRequest()
                {
                    Stmt = new Statement()
                    {
                        Sql = query.sql,
                        Args = GetArgs(query.args),
                    },
                    Type = PipelineRequestType.Execute,
                });
            }
            
            
            var postJson = JsonSerializer.Serialize(call);
            return postJson;
        }
        
        private string CreatePipelinecallAsJson(string query, IEnumerable<object>? args = null)
        {
            var call = new PipelineCall();
            call.Requests.Add(new PipelineRequest()
            {
                Stmt = new Statement()
                {
                    Sql = query,
                    Args = GetArgs(args),
                },
                Type = PipelineRequestType.Execute,
            });
            
            
            var postJson = JsonSerializer.Serialize(call);
            return postJson;
        }
        
        // TODO: move this somewhere else for cleaner code
        private List<LibSqlValue>? GetArgs(IEnumerable<object>? args)
        {
            // Avoid allocating a list if there are no args
            if (args == null || !args.Any())
                return [];
            
            var libSqlValues = new List<LibSqlValue>();
            foreach (var arg in args)
            {
                if (arg is LibSqlValue libSqlValue)
                {
                    libSqlValues.Add(libSqlValue);
                }
                else
                {
                    if (arg is double d)
                    {
                        libSqlValues.Add(new LibSqlValue()
                        {
                            Type = LibSqlValueType.Float,
                            Value = d
                        });
                    }
                    else if (arg is double f)
                    {
                        libSqlValues.Add(new LibSqlValue()
                        {
                            Type = LibSqlValueType.Float,
                            Value = f
                        });
                    }
                    else if (arg is int i)
                    {
                        libSqlValues.Add(new LibSqlValue()
                        {
                            Type = LibSqlValueType.Integer,
                            Value = i.ToString()
                        });
                    }
                    else if (arg is long l)
                    {
                        libSqlValues.Add(new LibSqlValue()
                        {
                            Type = LibSqlValueType.Integer,
                            Value = l.ToString()
                        });
                    }
                    else if (arg is byte[] b)
                    {
                        var base64 = Convert.ToBase64String(b);
                        libSqlValues.Add(new LibSqlValue()
                        {
                            Type = LibSqlValueType.Blob,
                            Base64 = b
                        });
                    }
                    else
                    {
                        libSqlValues.Add(new LibSqlValue()
                        {
                            Type = LibSqlValueType.Text,
                            Value = arg.ToString()
                        });
                    }
                }
            }
            
            return libSqlValues;
        }
    }
}