using BookingService.API.Domain.Models;
using BookingService.API.Infrastructure.Data;
using BookingMicro.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.API.Consumers
{
    public class CancelBookingTimeoutConsumer : IConsumer<CancelBookingTimeout>
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<CancelBookingTimeoutConsumer> _logger;

        public CancelBookingTimeoutConsumer(IServiceProvider provider, ILogger<CancelBookingTimeoutConsumer> logger)
        {
            _provider = provider;
            _logger   = logger;
        }

        public async Task Consume(ConsumeContext<CancelBookingTimeout> context)
        {
            var msg = context.Message;

            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

            var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == msg.BookingId);
            if (booking == null)
                return;

            // If booking is still pending after 10 minutes, cancel it
            if (booking.Status == BookingStatus.Pending)
            {
                booking.Status     = BookingStatus.Cancelled;
                booking.CanceledAt = DateTime.UtcNow;
                booking.IsCanceled = true;
                await db.SaveChangesAsync();

                await context.Publish(new BookingCancelled
                {
                    BookingId  = booking.Id,
                    CanceledAt = booking.CanceledAt.Value,
                    RoomId     = booking.RoomId,
                    HasRefund  = false,
                    RefundErrorReason = "Timeout"
                });
            }
        }
    }
} 