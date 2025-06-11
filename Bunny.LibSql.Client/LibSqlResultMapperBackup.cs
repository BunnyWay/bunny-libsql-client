/*using System.Collections;
   using System.Globalization;
   using System.Reflection;
   using Bunny.LibSql.Client.HttpClientModels;
   using Bunny.LibSql.Client.Json;
   using Bunny.LibSql.Client.Json.Enums;
   using Bunny.LibSql.Client.LINQ;
   using Bunny.LibSql.Client.TypeHandling;
   
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
               // Contains all the primary keys found in this row
               var rowPrimaryKeys = new Dictionary<string, string>();
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
                   var primaryKeyValue = primaryKeyProperty.GetValue(mappedItem)!.ToString();
                   var joinMapKey = $"{type.FullName}_{primaryKeyValue}";
   
                   rowPrimaryKeys[type.FullName] = primaryKeyValue;
                   if (joinMapper.TryAdd(joinMapKey, mappedItem) && type == typeof(T))
                   {
                       result.Add((T)mappedItem);
                   }
   
                   if (i > 0)
                   {
                       var join = joins[i - 1];
                       AttachToExistingItems(join, joinMapper, rowPrimaryKeys, mappedItem);
                   }
               }
           }
   
           return result;
       }
   
       private static void AttachToExistingItems(JoinNavigation join, Dictionary<string, object> joinMapper, Dictionary<string, string> rowPrimaryKeys, object mappedItem)
       {
           if(join.DataProperty?.DeclaringType == null)
               return;
           
           var typeToMap = join.DataProperty.DeclaringType.FullName;
           var primaryKeyValue = rowPrimaryKeys[typeToMap];
           var parentId = $"{typeToMap}_{primaryKeyValue}";
           if (joinMapper.TryGetValue(parentId, out var parentItem))
           {
               if (join.DataPropertyIsList)
               {
                   var list = (IList)join.DataProperty.GetValue(parentItem);
                   list.Add(mappedItem);
               }
               else
               {
                   join.DataProperty.SetValue(parentItem, mappedItem);
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
                   LibSqlToNativeValueMapper.AssignLibSqlValueToNativeProperty(col.DeclType, pi, item, row);
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
   }*/