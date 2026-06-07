using Quartz;

namespace Example.IdentityService.Extensions;

public static class QuartzExtensions
{
    public static void ConfigureQuartz(this IServiceCollection services)
    {
        services.AddQuartz(options =>
        {
            options.UseSimpleTypeLoader();
            options.UseInMemoryStore();
        });
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    }
}