using Bunny.LibSql.Client;
using Bunny.LibSql.Client.Demo;
using Bunny.LibSql.Client.LINQ;

var dbUrl = "https://harevis-bunnynet.turso.io/";
var accessKey = "";
//var accessKey = "";

var db = new TestDd(dbUrl, accessKey);
await db.ApplyMigrationsAsync();

/*try
{
    await db.Client.QueryAsync("DROP TABLE Person");
}
catch (Exception ex)
{
    
}
try
{
    await db.Client.QueryAsync("DROP TABLE Product");
}
catch (Exception ex)
{
    
}*/

await db.ApplyMigrationsAsync();

/*await db.Client.QueryAsync("DELETE FROM Person");
await db.Client.QueryAsync("DELETE FROM Product");

await db.People.InsertAsync(new Person()
{
    name = "dejan",
    lastName = "gp"
});
await db.Products.InsertAsync(new Product()
{
    name = "bootie",
    person_id = "0"
});
await db.Products.InsertAsync(new Product()
{
    name = "meow",
    person_id = "0"
});*/


var wot = db.People.ToList();


/*wot = db.People
    .Include(e => e.products)
    .Include<Product>(b => b.descriptions)
    .ToList();*/

foreach (var person in wot)
{
    Console.WriteLine($"Person data {person.name}");
    foreach (var prod in person.products)
    {
        Console.WriteLine($"Products for {person.name}: {prod.name}");
        foreach (var description in prod.descriptions)
        {
            Console.WriteLine($"Product description: {description.name}");
        }
    }
}
