using System.Collections;
using System.Globalization;
using System.Reflection;
using Bunny.LibSql.Client.HttpClientModels;
using Bunny.LibSql.Client.Json;
using Bunny.LibSql.Client.Json.Enums;
using Bunny.LibSql.Client.LINQ;

namespace Bunny.LibSql.Client;

public static class LibSqlResultMapper
{
    public static List<T> Map<T>(List<QueryCol> cols, List<List<LibSqlValue>> rows, List<JoinNavigation>? joins = null)
    {
        if (joins == null || joins.Count == 0)
        {
            return MapWithoutJoins<T>(cols, rows);
        }
        
        return MapWithJoins<T>(cols, rows, joins);
    }

    public static List<T> MapWithoutJoins<T>(List<QueryCol> cols, List<List<LibSqlValue>> rows)
    {
        var type = typeof(T);
        var result = new List<T>();
        foreach (var rowValues in rows)
        {
            var colIndex = 0;
            var mappedItem = ExtractNextItem(type, ref colIndex, cols, rowValues, false);
            if (mappedItem == null)
            {
                continue;
            }
            
            result.Add((T)mappedItem);
        }

        return result;
    }
    
    public static List<T> MapWithJoins<T>(List<QueryCol> cols, List<List<LibSqlValue>> rows, List<JoinNavigation> joins)
    {
        var mainType = typeof(T);
        // Create a type map
        var result = new List<T>();
        var joinMapper = new Dictionary<string, object>();
        foreach (var rowValues in rows)
        {
            int colIndex = 0;
            for (var i = 0; i < joins.Count + 1; i++)
            {
                var type = i == 0 ? mainType : joins[i - 1].RightDataType;
                var mappedItem = ExtractNextItem(type, ref colIndex, cols, rowValues, i > 0);
                if (mappedItem == null)
                {
                    continue;
                }

                var primaryKeyProperty = mappedItem.GetLibSqlPrimaryKeyProperty();
                var key = $"{type.FullName}_{primaryKeyProperty.GetValue(mappedItem)}";
                if (joinMapper.TryAdd(key, mappedItem) && type == typeof(T))
                {
                    result.Add((T)mappedItem);
                }

                if (i > 0)
                {
                    var join = joins[i - 1];
                    AttachToExistingItems(join, joinMapper, mappedItem);
                }
            }
        }

        return result;
    }

    private static void AttachToExistingItems(JoinNavigation join, Dictionary<string, object> joinMapper, object mappedItem)
    {
        var primaryKeyValue = join.RightProperty.GetValue(mappedItem);
        var parentyId = $"{join.LeftDataType.FullName}_{primaryKeyValue}";
        if (joinMapper.TryGetValue(parentyId, out var parentItem))
        {
            if (join.LeftPropertyIsList)
            {
                var list = (IList)join.LeftProperty.GetValue(parentItem);
                list.Add(mappedItem);
            }
            else
            {
                join.LeftProperty.SetValue(parentItem, mappedItem);
            }
        }
    }
    
    private static object? ExtractNextItem(Type type, ref int colIndex, List<QueryCol> cols, List<LibSqlValue> values, bool isJoin)
    {
        var item = Activator.CreateInstance(type);
        if(item == null) return null;
        
        var mappableProperties = type.GetLibSqlMappableProperties();

        int nullTypeCount = 0;
        int currentColsProcessed = 0;
        for (; colIndex < cols.Count; colIndex++)
        {
            var row = values[colIndex];
            var col = cols[colIndex];

            if (row.Type == LibSqlValueType.Null)
            {
                nullTypeCount++;
            }

            if (mappableProperties.TryGetValue(col.Name, out var pi))
            {
                AssignValue(pi, item, row);
            }
            
            currentColsProcessed++;
            if (currentColsProcessed == mappableProperties.Count)
            {
                break;
            }
        }

        colIndex++;
        if (currentColsProcessed == nullTypeCount && isJoin)
        {
            return null;
        }
        
        return item;
    }
    
    // TODO: move this to TypeHandling?
    private static void AssignValue(PropertyInfo pi, object obj, LibSqlValue libSqlValue)
    {
        switch (libSqlValue.Type)
        {
            case LibSqlValueType.Float:
                pi.SetValue(obj, libSqlValue.Value as double?);
                return;
            case LibSqlValueType.Text:
                pi.SetValue(obj, libSqlValue.Value.ToString());
                return;
            case LibSqlValueType.Integer:
                var intVal = libSqlValue.Value as int?;
                var intValParsed = intVal ?? 0;
                if (intVal == null && (libSqlValue?.Value == null || !int.TryParse(libSqlValue.Value?.ToString(), CultureInfo.InvariantCulture, out intValParsed)))
                {
                    return;
                }

                if (pi.PropertyType == typeof(bool))
                {
                    pi.SetValue(obj, intValParsed == 1);
                }
                else
                {
                    pi.SetValue(obj, intValParsed);
                }
                    
                return;
        }
    }
}