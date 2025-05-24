using BookingMicro.Contracts.Events;
using MassTransit;
using RoomService.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace RoomService.API.Consumers
{
    public class BookingCancelledConsumer : IConsumer<BookingCancelled>
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<BookingCancelledConsumer> _logger;

        public BookingCancelledConsumer(IServiceProvider provider, ILogger<BookingCancelledConsumer> logger)
        {
            _provider = provider;
            _logger   = logger;
        }

        public async Task Consume(ConsumeContext<BookingCancelled> context)
        {
            var msg = context.Message;
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<RoomDbContext>();

            // find room by booking? we only know bookingID not mapped; assume we set available by message.RoomId later; but not available.
            // we don't have RoomId in BookingCancelled message; so can't change IsAvailable.
            // So we rely on booking flow releasing eventually when booking removed etc.
            var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == msg.RoomId);
            if (room != null)
            {
                room.IsAvailable = true;
                await db.SaveChangesAsync();
            }
            _logger.LogInformation("BookingCancelled processed: room {RoomId} released", msg.RoomId);
        }
    }
} 