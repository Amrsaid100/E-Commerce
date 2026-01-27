using E_Commerce.DataContext;
using E_Commerce.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Repository
{
    public class GovernorateRepo : GenericRepo<Governorate>, IGovernorateRepo
    {
        private readonly EcommerceDbContext context;

        public GovernorateRepo(EcommerceDbContext context) : base(context)
        {
            this.context = context;
        }

        public async Task<List<Governorate>> GetAllGovernoratesAsync()
        {
            return await context.Governorates.ToListAsync();
        }

        public async Task<Governorate?> GetGovernorateByIdAsync(int id)
        {
            return await context.Governorates.FindAsync(id);
        }

        public async Task<bool> UpdateShippingCostAsync(int id, decimal shippingCost)
        {
            var governorate = await context.Governorates.FindAsync(id);
            if (governorate == null)
                return false;

            governorate.ShippingCost = shippingCost;
            await context.SaveChangesAsync();
            return true;
        }
    }
}
