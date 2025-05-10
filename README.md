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

## 🛠️ TODO / Roadmap

> **Note:** This library is currently a **Work In Progress (WIP)** prototype and not yet intended for production use. While foundational ORM and querying features are available, several important enhancements are still in progress.

### Planned Features

- **🔄 One-to-One Relationships**  
  Add support for modeling direct one-to-one relationships between entities using foreign keys.

- **🔁 Many-to-Many Relationships**  
  Implement support for many-to-many relationships via join tables and automated mapping.

- **💳 Transaction Support**  
  Introduce transaction handling to allow atomic multi-step operations.

- **🛡️ Error Handling**  
  Improve robustness by introducing consistent and developer-friendly error messages, exception types, and recovery suggestions.

- **📦 NuGet Package**  
  Package and publish the library to [NuGet.org](https://www.nuget.org/) for easier installation and versioning.

---

We welcome feedback, ideas, and contributions. If you're interested in helping shape the future of this library, feel free to open an issue or pull request!


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

The Bunny.LibSQL.Client ORM system uses attributes to define and control table structure, relationships, and query behavior. Here's a summary of the available attributes and their purpose:

| Attribute      | Description                                                                 |
|----------------|-----------------------------------------------------------------------------|
| `Table`        | Specifies a custom table name for the entity. If omitted, class name is used. |
| `Key`          | Marks the property as the primary key of the table.                         |
| `Index`        | Creates an index on the annotated property for faster lookups.              |
| `ForeignKey`   | Defines a relationship to another table by specifying the foreign key property name. |
| `AutoInclude`  | Enables eager loading of the related property automatically during queries. |


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
