using BookingService.API.Infrastructure.Data;
using BookingMicro.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using BookingService.API.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.API.Consumers
{
    public class RoomReserveRejectedConsumer : IConsumer<RoomReserveRejected>
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<RoomReserveRejectedConsumer> _logger;

        public RoomReserveRejectedConsumer(IServiceProvider provider, ILogger<RoomReserveRejectedConsumer> logger)
        {
            _provider = provider;
            _logger   = logger;
        }

        public async Task Consume(ConsumeContext<RoomReserveRejected> context)
        {
            var msg = context.Message;

            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

            var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == msg.BookingId);
            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found when processing RoomReserveRejected", msg.BookingId);
                return;
            }

            // If already cancelled or confirmed, ignore
            if (booking.Status != BookingStatus.Pending)
                return;

            booking.Status            = BookingStatus.Cancelled;
            booking.CanceledAt        = DateTime.UtcNow;
            booking.RefundErrorReason = msg.Reason;
            await db.SaveChangesAsync();

            await context.Publish(new BookingCancelled
            {
                BookingId  = booking.Id,
                CanceledAt = booking.CanceledAt.Value,
                RoomId     = booking.RoomId,
                HasRefund  = false,
                RefundErrorReason = booking.RefundErrorReason
            });
        }
    }
} 