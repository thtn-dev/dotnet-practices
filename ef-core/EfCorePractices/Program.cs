using EfCorePractices;
using EfCorePractices.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var connectionString = configuration["ConnectionStrings:DefaultConnection"]
    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");

var services = new ServiceCollection();

services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();

var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await context.Database.MigrateAsync();

//var products = Enumerable.Range(1, 10)
//    .Select(i => new Product
//    {
//        Name = $"Product {i}",
//        Price = i * 10m,
//        Currency = new Currency("USD", "United States Dollar")
//    })
//    .ToList();

//context.Products.AddRange(products);

await context.SaveChangesAsync();

var productsFromDb = await context.Products.ToListAsync();

productsFromDb.ForEach(p =>
{
    p.Currency = new Currency("VND", "Vietnamese Dong");
});

await context.SaveChangesAsync();
