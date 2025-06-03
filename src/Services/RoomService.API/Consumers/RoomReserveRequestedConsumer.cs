using BookingMicro.Contracts.Events;
using MassTransit;
using RoomService.API.Infrastructure.Data;
using RoomService.API.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace RoomService.API.Consumers
{
    public class RoomReserveRequestedConsumer : IConsumer<RoomReserveRequested>
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<RoomReserveRequestedConsumer> _logger;

        public RoomReserveRequestedConsumer(IServiceProvider provider, ILogger<RoomReserveRequestedConsumer> logger)
        {
            _provider = provider;
            _logger   = logger;
        }

        public async Task Consume(ConsumeContext<RoomReserveRequested> context)
        {
            var msg = context.Message;

            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<RoomDbContext>();

            var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == msg.RoomId);
            if (room == null)
            {
                await context.Publish(new RoomReserveRejected
                {
                    BookingId   = msg.BookingId,
                    RoomId      = msg.RoomId,
                    Reason      = "Room not found",
                    RejectedAt  = DateTime.UtcNow
                });
                return;
            }

            // NOTE: IsAvailable flag is removed from the logic - availability is controlled by checking overlapping dates in BookingService
            // Just publicising the successful event
            await db.SaveChangesAsync();

            await context.Publish(new RoomReserved
            {
                BookingId  = msg.BookingId,
                RoomId     = msg.RoomId,
                ReservedAt = DateTime.UtcNow
            });
        }
    }
} 