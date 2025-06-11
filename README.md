# 🐇 Bunny.LibSQL.Client for .NET

**An HTTP-based lightweight .NET LibSQL ORM client designed for performance and simplicity.**

Bunny.LibSQL.Client is a high-performance .NET client for [LibSQL](https://libsql.org/) that lets you define models, run queries, and use LINQ—without the bloat of heavyweight ORMs. Inspired by EF Core, reimagined for cloud-first applications.

---

## ✨ Features

- 🌐 HTTP-based access to LibSQL endpoints
- 🧠 Lightweight ORM
- ⚡ Async operations with `InsertAsync`, `QueryAsync`, and more
- 🔗 LINQ query support with `Include()` and `AutoInclude` for eager loading
- 🧱 Auto-migration via `ApplyMigrationsAsync`
- 🔄 Transaction support with `BeginTransactionAsync`, `CommitTransactionAsync`, and `RollbackTransactionAsync`
- 📦 Plug-and-play class-based DB structure
- 🌀 AI Embedding vector support
---

> **Note:** This library is currently a **Work In Progress (WIP)** prototype and not yet intended for production use. While foundational ORM and querying features are available, several important enhancements are still in progress.

We welcome feedback, ideas, and contributions. If you're interested in helping shape the future of this library, feel free to open an issue or pull request!

## 📦 Installation

[![NuGet](https://img.shields.io/nuget/v/Bunny.LibSql.Client.Demo.svg)](https://www.nuget.org/packages/Bunny.LibSql.Client.Demo/)

Install the package via NuGet:

```bash
dotnet add package Bunny.LibSql.Client.Demo
```

---

## 📚 Table of Contents

- [🏗️ Define Your Database](#️-define-your-database)
- [📐 Define Your Models](#-define-your-models)
- [⚙️ Initialize & Migrate](#️-initialize--migrate)
- [📥 Manage Records](#-insert-data)
  - [📥 Insert](#-insert)
  - [✏️ Update](#-update)
  - [❌ Delete](#-delete)
- [🔍 Query with LINQ](#-query-with-linq)
  - [Basic Query](#basic-query)
  - [Eager Loading with Include](#eager-loading-with-include)
- [🔄 Transactions](#-transactions)
- [⚡ Direct SQL Queries](#-direct-sql-queries)
  - [🧹 Run a command](#-run-a-command)
  - [🔢 Get a scalar value](#-get-a-scalar-value)
- [🧩 Attributes](#-attributes)
- [🧮 Supported Data Types](#-supported-data-types)
- [🧪 Sample Program](#-sample-program)


## 🏗️ Define Your Database

Start by inheriting from `LibSqlDatabase`. Use `LibSqlTable<T>` to define the tables.

```csharp
public class AppDb : LibSqlDatabase
{
    public AppDb(string dbUrl, string accessKey)
        : base(new LibSqlClient(dbUrl, accessKey)) {}

    public LibSqlTable<User> Users { get; set; }
    public LibSqlTable<Order> Orders { get; set; }
    public LibSqlTable<Product> Products { get; set; }
}
```

## 📐 Define Your Models
Your models should use standard C# classes. Use attributes to define relationships.

```csharp
[Table("Users")]
public class User
{
    [Key]
    public int id { get; set; }

    [Index]
    public string name { get; set; }

    [AutoInclude]
    public List<Order> Orders { get; set; } = new();
}

[Table("Orders")]
public class Order
{
    [Key]
    public int id { get; set; }

    [ForeignKeyFor(typeof(User))]
    public string user_id { get; set; }

    [AutoInclude]
    [ManyToMany(typeof(ProductOrder))]
    public List<Product> Product { get; set; }
}

[Table("ProductOrder")]
public class ProductOrder
{
    [Key]
    public string id { get; set; }

    [ForeignKeyFor(typeof(Order))]
    public string order_id { get; set; }

    [ForeignKeyFor(typeof(Product))]
    public string product_id { get; set; }
}

[Table("Products")]
public class Product
{
    [Key]
    public string id { get; set; }
    public string name { get; set; }
    [Unique]
    public string product_code { get; set; }
}
```

## ⚙️ Initialize & Migrate
Initialize your database and automatically sync models with ApplyMigrationsAsync.

```csharp
var db = new AppDb(dbUrl, accessKey);
await db.ApplyMigrationsAsync();
```

## 📥 Manage Records
You can easily insert, update, or delete records using InsertAsync, UpdateAsync, and DeleteAsync methods.

### 📥 Insert a record
Insert records using InsertAsync.
```csharp
await db.Users.UpdateAsync(new User
{
    id = "1",
    name = "Alice"
});
```

### ✏️ Update a record
Insert records using UpdateAsync.

```csharp
var user = await db.Users.Where(e => e.email == "super@bunny.net").FirstOrDefaultAsync();
user.email = "updated-super@bunny.net";
await db.Users.UpdateAsync(user);
```

### ❌ Delete a record
Delete records using DeleteAsync.

```csharp
var user = await db.Users.Where(e => e.email == "updated-super@bunny.net").FirstOrDefaultAsync();
await db.Users.DeleteAsync(user);
```

## 🔍 Query with LINQ

### Basic Query
```csharp
var users = db.Users
    .Where(u => u.name.StartsWith("A"))
    .ToListAsync();
```

### Eager Loading with Include 
```csharp
var usersWithOrders = db.Users
    .Include(u => u.Orders)
    .Include<Order>(o => o.Product)
    .FirstOrDefaultAsync();
```

### Aggregates: Count & Sum
You can perform aggregate queries such as CountAsync() and SumAsync(...). 
```csharp
var userCount = await db.Users.CountAsync();
var totalPrice = await db.Orders.SumAsync(o => o.price);
```
> ⚠️ **Important:** Always use the `Async` variants like `ToListAsync()`, `CountAsync()`, and `SumAsync(...)` to execute queries. Skipping the async call will **not** run the query.

## 🔄 Transactions

Use transactions to group multiple operations together. If something fails, you can roll back to ensure data consistency.

### 🚀 Begin a Transaction
```csharp
await db.Client.BeginTransactionAsync();
```


### ✅ Commit a Transaction
```csharp
await db.Client.CommitTransactionAsync();
```

### ❌ Rollback a Transaction
```csharp
await db.Client.RollbackTransactionAsync();
```

### 💡 Full transaction example
```csharp
await db.Client.BeginTransactionAsync();

try
{
    await db.People.InsertAsync(new Person
    {
        name = "dejan",
        lastName = "pelzel",
    });

    var inserted = await db.People.Where(e => e.name == "dejan5").FirstOrDefaultAsync();
    Console.WriteLine(inserted.id);

    await db.Client.CommitTransactionAsync();
}
catch
{
    await db.Client.RollbackTransactionAsync();
    throw;
}
```


## ⚡ Direct SQL Queries
For raw access, you can use the underlying client directly.

### 🧹 Run a command
```csharp
await db.Client.QueryAsync("DELETE FROM Users");
```

### 🔢 Get a scalar value
```csharp
var count = await db.Client.QueryScalarAsync<int>("SELECT COUNT(*) FROM Users");
```

## 🧩 Attributes

The Bunny.LibSQL.Client ORM system uses attributes to define and control table structure, relationships, and query behavior. Here's a summary of the available attributes and their purpose:

| Attribute      | Description                                                                 |
|----------------|-----------------------------------------------------------------------------|
| `Table`        | Specifies a custom table name for the entity. If omitted, class name is used. |
| `Key`          | Marks the property as the primary key of the table.                         |
| `Index`        | Creates an index on the annotated property for faster lookups.              |
| `ForeignKey`   | Defines a relationship to another table by specifying the foreign key property name. |
| `AutoInclude`  | Enables eager loading of the related property automatically during queries. |
| `Unique`       | Marks the field with the UNIQUE constraint, ensuring a unique value in every row. |


## 🧮 Supported Data Types

Bunny.LibSQL.Client automatically maps common C# types to supported LibSQL column types. These types are used for model properties and are inferred during table creation and querying.

| C# Type     | Description                              | Notes                                |
|-------------|------------------------------------------|--------------------------------------|
| `string`    | Textual data                             | Maps to `TEXT`                       |
| `int`       | 32-bit integer                           | Maps to `INTEGER`                    |
| `long`      | 64-bit integer                           | Maps to `INTEGER`                    |
| `double`    | Double-precision floating point          | Maps to `REAL`                       |
| `float`     | Single-precision floating point          | Maps to `REAL`                       |
| `decimal`   | Double-precision floating point          | Maps to `REAL`                       |
| `DateTime`  | Date and time representation             | Stored as `INTEGER` UNIX timestamp   |
| `bool`      | Boolean value                            | Stored as `0` (false) or `1` (true)  |
| `byte[]`    | Binary data (e.g., files, images)        | Maps to `BLOB`                       |
| `F32Blob`   | Vector F32 blob (e.g. ai embeddings      | Maps to `F32_BLOB`                   |

> ⚠️ **Note:** Nullable variants (e.g., `int?`, `bool?`, etc.) are also supported and will map to nullable columns.

## 🧪 Sample Program
```csharp
var db = new AppDb("https://your-libsql-instance.turso.io/", "your_access_key");
await db.ApplyMigrationsAsync();

await db.Users.InsertAsync(new User { id = "1", name = "Dejan" });

var users = await db.Users
    .Include(u => u.Orders)
    .Include<Order>(o => o.Product)
    .ToListAsync();

foreach (var user in users)
{
    Console.WriteLine($"User: {user.name}");
    foreach (var order in user.Orders)
    {
        Console.WriteLine($"  Ordered: {order.Product?.name}");
    }
}
```
