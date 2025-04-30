using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BookingService.API.Infrastructure.Clients
{
    public class RoomServiceClient : IRoomServiceClient
    {
        private readonly HttpClient _http;

        public RoomServiceClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<IEnumerable<RoomDto>> GetRoomsByHotelAsync(int hotelId)
        {
            var resp = await _http.GetAsync($"/api/Rooms?hotelId={hotelId}");
            resp.EnsureSuccessStatusCode();
            return (await resp.Content
                .ReadFromJsonAsync<IEnumerable<RoomDto>>())!;
        }

        public async Task<RoomDto> GetRoomByIdAsync(int roomId)
        {
            var resp = await _http.GetAsync($"/api/Rooms/{roomId}");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<RoomDto>();
        }
    }
}
