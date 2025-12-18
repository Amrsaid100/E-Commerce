
using E_Commerce.DataContext;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Repository
{
    public class GenericRepo<T> : IGenericRepo<T> where T : class
    {
        private readonly EcommerceDbContext context;

        public GenericRepo(EcommerceDbContext context)
        {
            this.context = context;
        }

        public GenericRepo() { 
        }
        public async Task AddAsync(T obj)
        {
            await context.Set<T>().AddAsync(obj);
        }

        public async Task DeleteAsync(T obj)
        {
            context.Set<T>().Remove(obj);
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await context.Set<T>().ToListAsync();
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await context.Set<T>().FindAsync(id);
        }

        public async Task UpdatdeAsync(T obj)
        {
            context.Set<T>().Update(obj);
        }
    }
}
