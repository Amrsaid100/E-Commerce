namespace E_Commerce.Services.PaymentService
{
    public class PaymentSer: IPaymentService
    {
        public async Task<string> CreatePaymentUrl(int orderId)
        {
           
            return $"https://fake-payment.com/pay?orderId={orderId}";
        }
    }
}
