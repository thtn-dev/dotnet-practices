using EfCorePractices.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCorePractices;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().OwnsOne(p => p.Currency, b =>
        {
            b.ToTable("Currencies");
        });
    }
}