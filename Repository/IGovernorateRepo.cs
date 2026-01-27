using E_Commerce.Entities;

namespace E_Commerce.Repository
{
    public interface IGovernorateRepo
    {
        Task<List<Governorate>> GetAllGovernoratesAsync();
        Task<Governorate?> GetGovernorateByIdAsync(int id);
        Task<bool> UpdateShippingCostAsync(int id, decimal shippingCost);
    }
}
