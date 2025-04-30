using BookingService.API.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.API.Infrastructure.Data
{
  public class BookingDbContext : DbContext
  {
    public BookingDbContext(DbContextOptions<BookingDbContext> opts) : base(opts) { }

    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder b)
    {
      base.OnModelCreating(b);
      b.Entity<Booking>().HasKey(x => x.Id);
    }
  }
}
