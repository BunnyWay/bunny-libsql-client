# .NET LibSQL Client

A lightweight HTTP-based .NET client for LibSQL that provides basic ORM capabilities, raw query execution, and LINQ-style querying support.

---

## Table of Contents

* [Installation](#installation)
* [Getting Started](#getting-started)
* [Basic ORM Support](#basic-orm-support)

  * [Defining Your Database](#defining-your-database)
  * [Applying Migrations](#applying-migrations)
  * [Inserting Data](#inserting-data)
  * [Querying Data](#querying-data)
  * [Defining Relationships](#defining-relationships)
  * [Auto-Includes (Eager Loading)](#auto-includes-eager-loading)
* [Raw Query Execution](#raw-query-execution)
* [Demo Project Example](#demo-project-example)
* [Contributing](#contributing)
* [License](#license)

---

## Installation

Install via NuGet:

```bash
dotnet add package Bunny.LibSql.Client
```

Or via the Package Manager:

```powershell
Install-Package Bunny.LibSql.Client
```

---

## Getting Started

1. **Create a subclass of `LibSqlDatabase`**

   ```csharp
   public class MyDatabase : LibSqlDatabase
   {
       public MyDatabase(string dbUrl, string accessKey)
           : base(new LibSqlClient(dbUrl, accessKey))
       {
       }

       public LibSqlTable<Person> People { get; set; }
       public LibSqlTable<Product> Products { get; set; }
       public LibSqlTable<Description> Descriptions { get; set; }
   }
   ```

2. **Instantiate and apply migrations**

   ```csharp
   var dbUrl = "https://your-libsql-endpoint/";
   var accessKey = "your-access-key";

   var db = new MyDatabase(dbUrl, accessKey);
   await db.ApplyMigrationsAsync();
   ```

---

## Basic ORM Support

### Defining Your Database

Your `LibSqlDatabase` subclass should expose tables as properties of type `LibSqlTable<T>`, where `T` is your entity class.

### Applying Migrations

`ApplyMigrationsAsync` will examine your entity definitions and automatically create, update, or drop tables/columns to match:

```csharp
await db.ApplyMigrationsAsync();
```

### Inserting Data

Call `InsertAsync` on a table:

```csharp
await db.People.InsertAsync(new Person {
    name = "Dejan",
    lastName = "gp"
});
```

### Querying Data

Use LINQ-style syntax. For basic queries:

```csharp
var allPeople = db.People.ToList();
```

To filter:

```csharp
var editors = db.People.Where(p => p.role == "Editor").ToList();
```

### Defining Relationships

Annotate navigation properties with `[ForeignKey]`:

```csharp
public class Person {
    public string id { get; set; }
    public string name { get; set; }

    [ForeignKey("person_id")]
    public List<Product> products { get; set; } = new();
}
```

The string argument is the column name in the related table.

### Auto-Includes (Eager Loading)

To automatically include related entities by default, add `[AutoInclude]` to the navigation property:

```csharp
[AutoInclude]
[ForeignKey("person_id")]
public List<Product> products { get; set; } = new();
```

---

## Raw Query Execution

Sometimes you need to run SQL directly:

```csharp
// Executes a command that does not return rows
await db.Client.QueryAsync("DELETE FROM Person WHERE active = FALSE");

// Executes a command and returns a single scalar value
int deletedCount = await db.Client.QueryScalarAsync<int>("DELETE FROM Person WHERE active = FALSE");
```

---

## Demo Project Example

Below is a minimal console demo that showcases migrations, inserts, and querying with includes:

```csharp
using Bunny.LibSql.Client;
using Bunny.LibSql.Client.LINQ;

var dbUrl = "https://harevis-bunnynet.turso.io/";
var accessKey = "<your-access-key>";

var db = new TestDd(dbUrl, accessKey);
await db.ApplyMigrationsAsync();

// Insert sample data
await db.People.InsertAsync(new Person { name = "dejan", lastName = "gp" });
await db.Products.InsertAsync(new Product { name = "cdn", person_id = "0" });
await db.Products.InsertAsync(new Product { name = "dns",   person_id = "0" });

// Query with includes
var peopleWithProducts = db.People
    .Include(p => p.products)
    .Include<Product>(prod => prod.descriptions)
    .ToList();

foreach (var person in peopleWithProducts) {
    Console.WriteLine($"Person: {person.name}");
    foreach (var prod in person.products) {
        Console.WriteLine($"  Product: {prod.name}");
        foreach (var desc in prod.descriptions) {
            Console.WriteLine($"    Description: {desc.name}");
        }
    }
}
```

---

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/new-feature`
3. Commit your changes: `git commit -m "Add awesome feature"`
4. Push to the branch: `git push origin feature/new-feature`
5. Open a pull request

Please follow the existing code style and include unit tests.

---

## License

This project is licensed under the MIT License. See [LICENSE](./LICENSE) for details.
