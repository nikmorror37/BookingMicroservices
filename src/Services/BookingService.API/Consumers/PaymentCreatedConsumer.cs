using BookingService.API.Infrastructure.Data;
using BookingMicro.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using BookingService.API.Domain.Models;

namespace BookingService.API.Consumers
{
    public class PaymentCreatedConsumer : IConsumer<PaymentCreated>
    {
        private readonly IServiceProvider? _serviceProvider;
        private readonly ILogger<PaymentCreatedConsumer>? _logger;

        public PaymentCreatedConsumer()
        {
            // This constructor is required by MassTransit
        }

        public PaymentCreatedConsumer(IServiceProvider serviceProvider, ILogger<PaymentCreatedConsumer> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentCreated> context)
        {
            if (_serviceProvider == null || _logger == null)
            {
                throw new InvalidOperationException("Consumer was not properly initialized with dependencies");
            }

            var message = context.Message;
            _logger.LogInformation("Received PaymentCreated event for booking {BookingId} with payment {PaymentId} and status {Status}", 
                message.BookingId, message.PaymentId, message.Status);

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

            var booking = await db.Bookings.FindAsync(message.BookingId);
            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found", message.BookingId);
                return;
            }

            _logger.LogInformation("Found booking {BookingId}, current status: {Status}, current payment ID: {PaymentId}", 
                booking.Id, booking.Status, booking.PaymentId);

            // always store payment id
            booking.PaymentId = message.PaymentId;

            const int PaymentCompletedStatus = 1; // see PaymentStatus.Completed enum in PaymentService

            if (message.Status == PaymentCompletedStatus && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.RefundError))
            {
                booking.Status = BookingStatus.Confirmed;
                booking.RefundErrorReason = null;
                await db.SaveChangesAsync();

                _logger.LogInformation("Booking {BookingId} confirmed after successful payment {PaymentId}",
                    message.BookingId, message.PaymentId);
            }
            else
            {
                await db.SaveChangesAsync();
                _logger.LogInformation("Stored payment {PaymentId} for booking {BookingId} with payment status {PaymentStatus}. Booking state left unchanged.",
                    message.PaymentId, message.BookingId, message.Status);
            }
        }
    }
} 