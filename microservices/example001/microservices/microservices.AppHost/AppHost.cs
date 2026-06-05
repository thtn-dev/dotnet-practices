using CommunityToolkit.Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

var solutionRoot = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, ".."));
var daprConfig = Path.Combine(solutionRoot, "dapr", "config.yaml");
var daprComponents = Path.Combine(solutionRoot, "dapr", "components");

var placement = builder.AddContainer("dapr-placement", "daprio/dapr", "1.17.9")
    .WithArgs("./placement", "--port", "50005")
    .WithEndpoint(port: 50005, targetPort: 50005, name: "grpc", isProxied: false);

var scheduler = builder.AddContainer("dapr-scheduler", "daprio/dapr", "1.17.9")
    .WithArgs("./scheduler", "--port", "50006", "--etcd-data-dir", "/tmp/dapr-scheduler-data")
    .WithEndpoint(port: 50006, targetPort: 50006, name: "grpc", isProxied: false);

DaprSidecarOptions SidecarOptions(string appId) => new()
{
    AppId = appId,
    Config = daprConfig,
    ResourcesPaths = [daprComponents],
    PlacementHostAddress = "localhost:50005",
    SchedulerHostAddress = "localhost:50006",
    EnableApiLogging = true,
};

builder.AddProject<Projects.Example_ApiGateway>("example-apigateway")
    .WithExternalHttpEndpoints()
    .WaitFor(placement)
    .WaitFor(scheduler)
    .WithDaprSidecar(SidecarOptions("api-gateway"));

builder.AddProject<Projects.Example_AuthService>("example-authservice")
    .WaitFor(placement)
    .WaitFor(scheduler)
    .WithDaprSidecar(SidecarOptions("auth-service"));

builder.AddProject<Projects.Example_WorkerService>("example-workerservice")
    .WaitFor(placement)
    .WaitFor(scheduler)
    .WithDaprSidecar(SidecarOptions("worker-service"));

builder.Build().Run();
