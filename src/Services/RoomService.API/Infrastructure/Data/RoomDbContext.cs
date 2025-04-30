using Microsoft.EntityFrameworkCore;
using RoomService.API.Domain.Models;

namespace RoomService.API.Infrastructure.Data
{
    public class RoomDbContext : DbContext
    {
        public RoomDbContext(DbContextOptions<RoomDbContext> options)
            : base(options)
        { }

        public DbSet<Room> Rooms => Set<Room>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Room>().HasKey(r => r.Id);

            builder.Entity<Room>()
                   .Property(r => r.Price)
                   .HasColumnType("decimal(18,2)");
        }
    }
}