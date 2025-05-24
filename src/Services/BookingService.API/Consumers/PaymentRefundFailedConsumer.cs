using BookingService.API.Domain.Models;
using BookingService.API.Infrastructure.Data;
using BookingMicro.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.API.Consumers
{
    public class PaymentRefundFailedConsumer : IConsumer<PaymentRefundFailed>
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<PaymentRefundFailedConsumer> _logger;

        public PaymentRefundFailedConsumer(IServiceProvider provider, ILogger<PaymentRefundFailedConsumer> logger)
        {
            _provider = provider;
            _logger   = logger;
        }

        public async Task Consume(ConsumeContext<PaymentRefundFailed> context)
        {
            var msg = context.Message;
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

            var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == msg.BookingId);
            if (booking == null) return;

            booking.Status            = BookingStatus.RefundError;
            booking.RefundErrorReason = msg.Reason;
            await db.SaveChangesAsync();

            _logger.LogWarning("Refund failed for booking {BookingId}: {Reason}", msg.BookingId, msg.Reason);
        }
    }
} 