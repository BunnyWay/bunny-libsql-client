# 🐇 Bunny.LibSQL.Client for .NET

**An HTTP-based lightweight .NET LibSQL ORM client designed for performance and simplicity.**

Bunny.LibSQL.Client is a high-performance .NET client for [LibSQL](https://libsql.org/) that lets you define models, run queries, and use LINQ—without the bloat of heavyweight ORMs. Inspired by EF Core, reimagined for cloud-first applications.

---

## ✨ Features

- 🌐 HTTP-based access to LibSQL endpoints
- 🧠 Lightweight ORM-like structure
- ⚡ Async operations with `InsertAsync`, `QueryAsync`, and more
- 🔗 LINQ query support with `Include()` and `AutoInclude` for eager loading
- 🧱 Auto-migration via `ApplyMigrationsAsync`
- 📦 Plug-and-play class-based DB structure

---

## 🚀 Getting Started

### 📦 Installation

> Coming soon via NuGet: `Bunny.LibSQL.Client`

For now, clone this repo and include the project in your solution.

---

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
    [ForeignKey("user_id")]
    public List<Order> Orders { get; set; } = new();
}

[Table("Orders")]
public class Order
{
    [Key]
    public int id { get; set; }
    public string user_id { get; set; }
    public string product_id { get; set; }

    [AutoInclude]
    [ForeignKey("product_id")]
    public Product Product { get; set; }
}

[Table("Products")]
public class Product
{
    [Key]
    public string id { get; set; }
    public string name { get; set; }
}
```

## ⚙️ Initialize & Migrate
Initialize your database and automatically sync models with ApplyMigrationsAsync.

```csharp
var db = new AppDb(dbUrl, accessKey);
await db.ApplyMigrationsAsync();
```

## 📥 Insert Data
Insert records using InsertAsync.

```csharp
await db.Users.InsertAsync(new User
{
    id = "1",
    name = "Alice"
});

await db.Products.InsertAsync(new Product
{
    id = "p1",
    name = "Carrot Sneakers"
});
```

## 🔍 Query with LINQ

### Basic Query
```csharp
var users = db.Users
    .Where(u => u.name.StartsWith("A"))
    .ToList();
```

### Eager Loading with Include 
```csharp
var usersWithOrders = db.Users
    .Include(u => u.Orders)
    .Include<Order>(o => o.Product)
    .ToList();
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

Attribute	Purpose
Key - Defines the primary key for the table
ForeignKey -	Defines a foreign key relation based on a property name
AutoInclude -	Automatically includes the related entities on query

## 🧪 Sample Program
```csharp
var db = new AppDb("https://your-libsql-instance.turso.io/", "your_access_key");
await db.ApplyMigrationsAsync();

await db.Users.InsertAsync(new User { id = "1", name = "Dejan" });

var users = db.Users
    .Include(u => u.Orders)
    .Include<Order>(o => o.Product)
    .ToList();

foreach (var user in users)
{
    Console.WriteLine($"User: {user.name}");
    foreach (var order in user.Orders)
    {
        Console.WriteLine($"  Ordered: {order.Product?.name}");
    }
}
```
