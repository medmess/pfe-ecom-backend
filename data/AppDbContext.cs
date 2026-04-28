using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using pfe.ecom.api.Models;

namespace pfe.ecom.api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
  {
  }

  public DbSet<Product> Products { get; set; }
  public DbSet<Order> Orders { get; set; }
  public DbSet<OrderItem> OrderItems { get; set; }
  public DbSet<Payment> Payments { get; set; }

  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    builder.Entity<Product>()
        .Property(p => p.Price)
        .HasPrecision(18, 2);

    builder.Entity<Order>()
        .Property(o => o.TotalAmount)
        .HasPrecision(18, 2);

    builder.Entity<OrderItem>()
        .Property(oi => oi.UnitPrice)
        .HasPrecision(18, 2);

    builder.Entity<Payment>()
        .Property(p => p.Amount)
        .HasPrecision(18, 2);

    builder.Entity<Product>()
        .HasOne(p => p.Supplier)
        .WithMany()
        .HasForeignKey(p => p.SupplierId)
        .OnDelete(DeleteBehavior.SetNull);

    builder.Entity<Order>()
        .HasOne(o => o.Payment)
        .WithOne(p => p.Order)
        .HasForeignKey<Payment>(p => p.OrderId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
