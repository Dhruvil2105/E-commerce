using ECommerce.Inventory.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Data
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
        {

        }

        public DbSet<StockItem> StockItems => Set<StockItem>();
        public DbSet<StockReservation> stockReservations => Set<StockReservation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<StockItem>(entity =>
            {
                entity.ToTable("StockItems");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ProductName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.TenantId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(e => new { e.ProductId, e.TenantId })
                    .IsUnique();

                entity.HasIndex(e => e.TenantId);
            });

            modelBuilder.Entity<StockReservation>(entity =>
            {
                entity.ToTable("StockReservations");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.FailureReason)
                    .HasMaxLength(500);

                entity.HasIndex(e => e.OrderId)
                    .IsUnique();
            });
        }
    }
}
