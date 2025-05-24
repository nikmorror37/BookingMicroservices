using MassTransit;
using Microsoft.EntityFrameworkCore;
using BookingMicro.Contracts.Events;
using PaymentService.API.Infrastructure.Data;
using PaymentService.API.Domain.Models;
using PaymentService.API.Infrastructure.Clients;
using Microsoft.Extensions.Logging;

namespace PaymentService.API.Consumers
{
    public class BookingCancelledConsumer : IConsumer<BookingCancelled>
    {
        private readonly PaymentDbContext _db;
        private readonly IPaymentGatewayClient _gateway;
        private readonly ILogger<BookingCancelledConsumer> _logger;

        public BookingCancelledConsumer(PaymentDbContext db, IPaymentGatewayClient gateway, ILogger<BookingCancelledConsumer> logger)
        {
            _db = db;
            _gateway = gateway;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<BookingCancelled> context)
        {
            var msg = context.Message;

            // If booking had no payment (e.g. expired before user paid) – nothing to refund
            if (!msg.HasRefund)
            {
                _logger.LogInformation("Booking {BookingId} cancelled without completed payment, refund not required", msg.BookingId);
                return;
            }

            // find the completed payment by bookingId
            var payment = await _db.Payments
                .FirstOrDefaultAsync(p => p.BookingId == msg.BookingId && p.Status == PaymentStatus.Completed);

            if (payment == null)
            {
                // no payment — publish an error
                await context.Publish(new PaymentRefundFailed
                {
                    PaymentId  = 0,
                    BookingId  = msg.BookingId,
                    Reason     = "No payment to refund",
                    FailedAt   = DateTime.UtcNow
                });
                return;
            }

            // try to refund money through the bank gateway
            bool ok;
            try
            {
                ok = await _gateway.RefundAsync(payment.Id, payment.Amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for booking {BookingId}", msg.BookingId);
                throw;
            }

            if (!ok)
            {
                payment.Status = PaymentStatus.RefundError;
                await _db.SaveChangesAsync();

                await context.Publish(new PaymentRefundFailed
                {
                    PaymentId  = payment.Id,
                    BookingId  = payment.BookingId,
                    Reason     = "Gateway refund failed",
                    FailedAt   = DateTime.UtcNow
                });
                return;
            }

            // successful refund
            payment.Status     = PaymentStatus.Refunded;
            payment.RefundedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await context.Publish(new PaymentRefunded
            {
                PaymentId   = payment.Id,
                BookingId   = payment.BookingId,
                RefundedAt  = payment.RefundedAt.Value
            });
        }
    }
}
