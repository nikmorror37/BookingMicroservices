namespace PaymentService.API.Infrastructure.Clients
{
    public interface IPaymentGatewayClient
    {
        /// <summary>
        /// Process a payment through the payment gateway
        /// </summary>
        /// <param name="paymentId">The ID of the payment to process</param>
        /// <param name="amount">The amount to process</param>
        /// <returns>True if the payment was successful, false otherwise</returns>
        Task<bool> ProcessPaymentAsync(int paymentId, decimal amount);

        /// <summary>
        /// Try to return the specified amount on a paymentId transaction.
        /// </summary>
        Task<bool> RefundAsync(int paymentId, decimal amount);
    }
}
