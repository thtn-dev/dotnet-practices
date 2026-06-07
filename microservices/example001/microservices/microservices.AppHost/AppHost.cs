var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Example_ApiGateway>("example-api-gateway")
    .WithHttpEndpoint(port: 8080, name: "gateway");

builder.AddProject<Projects.Example_IdentityService>("example-identity-service");

builder.AddProject<Projects.Example_WorkerService>("example-worker-service");

builder.Build().Run();
