using BookingService.API.Domain.Models;
using BookingService.API.Infrastructure.Data;
using BookingMicro.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.API.Consumers
{
    public class PaymentRefundedConsumer : IConsumer<PaymentRefunded>
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<PaymentRefundedConsumer> _logger;

        public PaymentRefundedConsumer(IServiceProvider provider, ILogger<PaymentRefundedConsumer> logger)
        {
            _provider = provider;
            _logger   = logger;
        }

        public async Task Consume(ConsumeContext<PaymentRefunded> context)
        {
            var msg = context.Message;
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

            var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == msg.BookingId);
            if (booking == null) return;

            booking.Status            = BookingStatus.Cancelled;
            booking.RefundErrorReason = null;
            await db.SaveChangesAsync();

            _logger.LogInformation("Booking {BookingId} marked Cancelled after refund", msg.BookingId);
        }
    }
} 