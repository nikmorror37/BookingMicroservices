using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace BookingService.API.Infrastructure.Clients
{
    public class PaymentServiceClient : IPaymentServiceClient
    {
        private readonly HttpClient _http;
        public PaymentServiceClient(HttpClient http) => _http = http;

        public async Task<bool> RefundAsync(int paymentId)
        {
            var resp = await _http.PostAsync($"/api/payments/{paymentId}/refund", null);
            return resp.IsSuccessStatusCode;
        }
    }
}