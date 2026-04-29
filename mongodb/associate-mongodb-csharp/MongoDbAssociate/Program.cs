using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDbAssociate.Entities;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var connectionString = configuration["MongoDb:ConnectionString"]
                       ?? throw new InvalidOperationException("Missing MongoDb:ConnectionString");
var databaseName = configuration["MongoDb:DatabaseName"]
                   ?? throw new InvalidOperationException("Missing MongoDb:DatabaseName");

var services = new ServiceCollection();

services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
services.AddKeyedSingleton<IMongoDatabase>(databaseName, (sp, _) =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));


await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();

var database = scope.ServiceProvider.GetRequiredKeyedService<IMongoDatabase>(databaseName);
Console.WriteLine($"MongoDB database ready: {database.DatabaseNamespace.DatabaseName}");

var collection = database.GetCollection<Listing>("listingsAndReviews");

var result = await collection.Aggregate()
    .Group(x => x.Address.Country, g => new
    {
        Country = g.Key,
        AvgPrice = g.Average(x => x.Price),
        MinPrice = g.Min(x => x.Price),
        MaxPrice = g.Max(x => x.Price),
        Total = g.Count()
    })
    .SortByDescending(x => x.Total)
    .ToListAsync();

var d = 16;