using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace BookingService.API.Infrastructure.Clients
{
    public class HotelServiceClient : IHotelServiceClient
    {
        private readonly HttpClient _http;
        public HotelServiceClient(HttpClient http) => _http = http;

        public async Task<HotelDto> GetHotelByIdAsync(int hotelId)
        {
            var resp = await _http.GetAsync($"/api/hotels/{hotelId}");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<HotelDto>()!;
        }
    }
}
