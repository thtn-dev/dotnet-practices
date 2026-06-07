using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Example.IdentityService.Endpoints;

/// <summary>
///     Interface for defining API endpoints in a modular way.
/// </summary>
public interface IEndpoint
{
    /// <summary>
    ///     Map the endpoint to the application's routing system.
    /// </summary>
    /// <param name="app"></param>
    void MapEndpoint(WebApplication app);
}

/// <summary>
///     Interface for endpoints that belong to a route group.
///     Allows grouping endpoints under a common prefix with shared configuration.
/// </summary>
public interface IEndpointGroup
{
    /// <summary>
    ///     The route prefix for this group (e.g., "/api/users")
    /// </summary>
    string GroupPrefix { get; }

    /// <summary>
    ///     Map all endpoints within this group
    /// </summary>
    void MapEndpoints(RouteGroupBuilder group);
}

public static class MapEndpointExtensions
{
    /// <summary>
    ///     Registers all IEndpoint and IEndpointGroup implementations from the assembly containing T.
    /// </summary>
    public static IServiceCollection RegisterEndpointsFromAssemblyContaining<T>(this IServiceCollection services)
    {
        var assembly = typeof(T).Assembly;

        var endpointTypes = assembly.GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IEndpoint)) &&
                        t is { IsClass: true, IsAbstract: false, IsInterface: false });

        var endpointDescriptors = endpointTypes
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
            .ToArray();

        services.TryAddEnumerable(endpointDescriptors);

        var groupTypes = assembly.GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IEndpointGroup)) &&
                        t is { IsClass: true, IsAbstract: false, IsInterface: false });

        var groupDescriptors = groupTypes
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpointGroup), type))
            .ToArray();

        services.TryAddEnumerable(groupDescriptors);

        return services;
    }

    /// <summary>
    ///     Maps all registered IEndpoint and IEndpointGroup implementations.
    /// </summary>
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        // Map individual endpoints
        var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();
        foreach (var endpoint in endpoints) endpoint.MapEndpoint(app);

        // Map endpoint groups
        var groups = app.Services.GetRequiredService<IEnumerable<IEndpointGroup>>();
        foreach (var group in groups)
        {
            var routeGroup = app.MapGroup(group.GroupPrefix)
                .WithTags(group.GroupPrefix);
            group.MapEndpoints(routeGroup);
        }

        return app;
    }
}