namespace E_Commerce.Services.PaymentService
{
    public interface IPaymentService
    {
        Task<string> CreatePaymentUrl(int orderId);
    }

}
