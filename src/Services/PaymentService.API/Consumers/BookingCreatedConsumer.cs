using BookingMicro.Contracts.Events;
using MassTransit;
using PaymentService.API.Domain.Models;
using PaymentService.API.Infrastructure.Clients;
using PaymentService.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PaymentService.API.Consumers
{
    public class BookingCreatedConsumer : IConsumer<BookingCreated>
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<BookingCreatedConsumer> _logger;
        private readonly IRoomServiceClient _roomClient;
        private readonly IPaymentGatewayClient _gateway;

        public BookingCreatedConsumer(IServiceProvider provider,
            ILogger<BookingCreatedConsumer> logger,
            IRoomServiceClient roomClient,
            IPaymentGatewayClient gateway)
        {
            _provider   = provider;
            _logger     = logger;
            _roomClient = roomClient;
            _gateway    = gateway;
        }

        public async Task Consume(ConsumeContext<BookingCreated> context)
        {
            var msg = context.Message;

            // calculate amount using RoomService
            var room = await _roomClient.GetRoomByIdAsync(msg.RoomId);
            if (room == null)
            {
                _logger.LogWarning("Room {RoomId} not found while processing BookingCreated", msg.RoomId);
                return;
            }
            var nights = (msg.CheckOut.Date - msg.CheckIn.Date).Days;
            if (nights <= 0) nights = 1;
            var amount = room.Price * nights;

            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

            // idempotency: check if payment already exists
            var existing = await db.Payments
                .FirstOrDefaultAsync(p => p.BookingId == msg.BookingId);
            if (existing != null)
            {
                _logger.LogInformation("Payment already exists for booking {BookingId}", msg.BookingId);
                return;
            }

            var payment = new Payment
            {
                BookingId = msg.BookingId,
                Amount    = amount,
                Status    = PaymentStatus.Pending,
                PaidAt    = DateTime.UtcNow,
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            // Process via gateway
            await db.SaveChangesAsync();
            // NOTE: Payment will be processed later by user explicit action (see PaymentsController.Pay endpoint)
        }
    }
} 