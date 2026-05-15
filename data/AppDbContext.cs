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
  public DbSet<OrderShippingInfo> OrderShippingInfos { get; set; }
  public DbSet<Payment> Payments { get; set; }
  public DbSet<SupplierOffer> SupplierOffers { get; set; }
  public DbSet<SupplyRequest> SupplyRequests { get; set; }

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

    builder.Entity<SupplierOffer>()
        .Property(p => p.UnitPrice)
        .HasPrecision(18, 2);

    builder.Entity<SupplyRequest>()
        .Property(r => r.TotalAmount)
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

    builder.Entity<Order>()
        .HasOne(o => o.ShippingInfo)
        .WithOne(s => s.Order)
        .HasForeignKey<OrderShippingInfo>(s => s.OrderId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.Entity<OrderShippingInfo>()
        .HasIndex(s => s.OrderId)
        .IsUnique();

    builder.Entity<SupplierOffer>()
        .HasOne(o => o.Provider)
        .WithMany()
        .HasForeignKey(o => o.ProviderId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.Entity<SupplyRequest>()
        .HasOne(r => r.SupplierOffer)
        .WithMany()
        .HasForeignKey(r => r.SupplierOfferId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.Entity<SupplyRequest>()
        .HasOne(r => r.Dealer)
        .WithMany()
        .HasForeignKey(r => r.DealerId)
        .OnDelete(DeleteBehavior.Restrict);
  }
}
