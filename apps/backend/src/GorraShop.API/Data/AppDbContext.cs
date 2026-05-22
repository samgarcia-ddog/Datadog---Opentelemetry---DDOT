using GorraShop.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GorraShop.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product>  Products   => Set<Product>();
    public DbSet<Order>    Orders     => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Category>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
        });

        model.Entity<Product>(e =>
        {
            e.Property(p => p.Price).HasPrecision(10, 2);
            e.HasOne(p => p.Category)
             .WithMany(c => c.Products)
             .HasForeignKey(p => p.CategoryId);
        });

        model.Entity<Order>(e =>
        {
            e.Property(o => o.Total).HasPrecision(10, 2);
            e.HasMany(o => o.Items)
             .WithOne(i => i.Order)
             .HasForeignKey(i => i.OrderId);
        });

        model.Entity<OrderItem>(e =>
        {
            e.Property(i => i.UnitPrice).HasPrecision(10, 2);
        });
    }
}
