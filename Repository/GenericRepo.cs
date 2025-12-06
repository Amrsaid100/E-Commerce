
using E_Commerce.DataContext;

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
        public Task AddAsync(T obj)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(T obj)
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<T> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task UpdatdeAsync(T obj)
        {
            throw new NotImplementedException();
        }
    }
}
