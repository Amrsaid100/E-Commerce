using E_Commerce.Entities;

namespace E_Commerce.Services.PayMob
{
    public interface IPaymobService
    {
        Task<string> CreatePaymentUrlAsync(Order order);
    }

}
