using Example.IdentityService.Data;
using Example.IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Example.IdentityService.Extensions;

public static class EfCoreExtensions
{
    public static void RegisterNpgSqlDbContexts<TAppDbContext>(this IServiceCollection services,
        string connectionString)
        where TAppDbContext : DbContext
    {
        services.AddDbContextPool<DbContext, TAppDbContext>((_, opts) =>
        {
            opts.UseNpgsql(connectionString,
                options => { options.MigrationsAssembly(typeof(Program).Assembly.FullName); });
        });
    }
    public static void ConfigureIdentity(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
    }
}