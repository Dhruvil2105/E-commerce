using ECommerce.Payment.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.Data
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
        {

        }

        public DbSet<PaymentRecord> payments => Set<PaymentRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PaymentRecord>(entity =>
            {
                entity.ToTable("Payments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount)
                    .HasPrecision(18, 2);

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.TenentId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ChargeId)
                    .HasMaxLength(200);

                entity.Property(e => e.FailureReason)
                    .HasMaxLength(500);

                entity.Property(e => e.Status)
                    .HasMaxLength(50);

                entity.HasIndex(e => e.OrderId)
                    .IsUnique();

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.TenentId);
            });
        }
    }
}
