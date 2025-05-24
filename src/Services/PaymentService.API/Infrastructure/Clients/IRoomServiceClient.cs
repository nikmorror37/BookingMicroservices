using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaymentService.API.Infrastructure.Clients
{
    public interface IRoomServiceClient
    {
        Task<RoomDto> GetRoomByIdAsync(int roomId);
        Task<IEnumerable<RoomDto>> GetRoomsByHotelAsync(int hotelId);
    }

    public class RoomDto
    {
        public int    Id          { get; set; }
        public int    HotelId     { get; set; }
        public string Number      { get; set; } = string.Empty;
        public decimal Price      { get; set; }
        public bool   IsAvailable { get; set; }
    }
} 