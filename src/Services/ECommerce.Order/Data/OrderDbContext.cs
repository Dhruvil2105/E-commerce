using ECommerce.Order.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace ECommerce.Order.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
        {

        }

        public DbSet<Models.Order> Orders => Set<Models.Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Models.Order>(entity =>
            {
               
                entity.ToTable("Orders");
                entity.HasKey( e => e.Id);
                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.TenantId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e=>e.Total)
                    .HasPrecision(18,2);

                entity.Property(e => e.CancellationReason)
                    .HasMaxLength(500);

                entity.Property(e => e.Status)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(e=>e.TrackingNumber)
                    .HasMaxLength(100);

                entity.Property(e => e.Courier)
                    .HasMaxLength(100);

                entity.HasMany(e => e.Items)
                    .WithOne(e => e.Order)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => new { e.UserId, e.TenantId });
                entity.HasIndex(e => e.Status);

            });

            modelBuilder.Entity<OrderItem>(entity =>
            {

                entity.ToTable("OrderItems");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.ProductName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.UnitPrice)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Subtotal)
                    .HasPrecision(18, 2);



            });

        }
    }
}
