namespace PaymentService.API.Infrastructure.Clients
{
    public interface IPaymentGatewayClient
    {
        /// try to return the specified amount on a paymentId transaction.
        Task<bool> RefundAsync(int paymentId, decimal amount);
    }
}
