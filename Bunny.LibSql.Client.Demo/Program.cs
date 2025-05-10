using Bunny.LibSql.Client;
using Bunny.LibSql.Client.Demo;
using Bunny.LibSql.Client.LINQ;

var dbUrl = "https://harevis-bunnynet.turso.io/";
var accessKey = "eyJhbGciOiJFZERTQSIsInR5cCI6IkpXVCJ9.eyJhIjoicnciLCJnaWQiOiJhYmNmODU0ZC01OTg0LTRhZjgtOGY2Mi1hYmViYzlmMDYxNDgiLCJpYXQiOjE3NDY4NTA0NTZ9.FCLeC9ReuBhSGj_jo_UOx5qBsPrE4Qt2pnOKv_zwEZm51e765gF2wuNDjYmKyVyWp8h3B6C6tc9reZyqbBcXBg";
//var accessKey = "eyJ0eXAiOiJKV1QiLCJhbGciOiJFZERTQSJ9.eyJwIjp7InJvIjpudWxsLCJydyI6eyJucyI6WyJyZXdyZXciXSwidGFncyI6bnVsbH0sInJvYSI6bnVsbCwicndhIjpudWxsLCJkZGwiOm51bGx9LCJpYXQiOjE3NDY3NjI0MzJ9.bDYrUYhhCphM2omQeEU4OUafUFLVzff5y_H-04hgFdsc6ZL0uXUNlVQDw0TvFQQgk8krJoG95YyZ1DKLU_bkCA";

var db = new TestDd(dbUrl, accessKey);
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
