using CatalogService.API.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.API.Infrastructure.Data
{
    public class CatalogDbContext : DbContext
    {
        public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
            : base(options) { }

        public DbSet<Hotel> Hotels => Set<Hotel>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Hotel>().HasKey(h => h.Id);
        }
    }
}
