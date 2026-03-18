using Microsoft.EntityFrameworkCore;

namespace ECommerce.Product.Data
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
        {

        }

        public DbSet<Models.Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Models.Product>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

                entity.Property(e => e.Description)
                .HasMaxLength(2000);

                entity.Property(e => e.Price)
                .HasPrecision(18,2);

                entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(100);

                entity.Property(e => e.ImageData)
                .IsRequired(false);

                entity.Property(e => e.ImageContentType)
                .IsRequired(false)
                .HasMaxLength(100);

                entity.Property(e => e.ImageFileName)
                .IsRequired(false)
                .HasMaxLength(256);

                entity.Property(e=>e.TenantId)
                .IsRequired()
                .HasMaxLength(100);

                // Indexes for common query patterns
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => new { e.TenantId, e.IsActive });
            });
        }
    }
}
