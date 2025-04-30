using PaymentService.API.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.API.Infrastructure.Data
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> opts)
            : base(opts) { }

        public DbSet<Payment> Payments => Set<Payment>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Payment>(b =>
		{
			b.HasKey(p => p.Id);

			b.Property(p => p.Amount)
			 .HasColumnType("decimal(18,4)")
			 //.HasPrecision(18, 4)
			 .IsRequired();
		});
        }
    }
}
