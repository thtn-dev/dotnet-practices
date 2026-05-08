using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDbAssociate.Entities.SampleAirbnb;

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

// var result = await collection.Aggregate()
//     .Group(x => x.Address.Country, g => new
//     {
//         Country = g.Key,
//         AvgPrice = g.Average(x => x.Price),
//         MinPrice = g.Min(x => x.Price),
//         MaxPrice = g.Max(x => x.Price),
//         Total = g.Count()
//     })
//     .SortByDescending(x => x.Total)
//     .ToListAsync();

var start = new DateTime(2016, 1, 1);  // ← sửa 2017 thành 2016
var end   = new DateTime(2019, 1, 1);

var pipeline = collection.Aggregate()
    .Match(x => x.Address.CountryCode == "US")
    .Limit(10)
    .Unwind(x => x.Reviews)
    .Match(new BsonDocument("reviews.date",
        new BsonDocument {
            { "$gte", start },
            { "$lt",  end }
        }))
    .Group(new BsonDocument
    {
        { "_id", "$_id" },
        { "doc", new BsonDocument("$first", "$$ROOT") },
        { "reviews", new BsonDocument("$push", "$reviews") }
    })
    .AppendStage<BsonDocument>(new BsonDocument("$replaceRoot",
        new BsonDocument("newRoot",
            new BsonDocument("$mergeObjects", new BsonArray
            {
                "$doc",
                new BsonDocument("reviews", "$reviews")
            }))))
    .As<Listing>();

var result = await pipeline.ToListAsync();
var d = 16;