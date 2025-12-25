
namespace E_Commerce.Repository
{
    public interface IGenericRepo<T> where T : class
    {
        Task AddAsync(T obj);
        Task DeleteAsync(T obj);
        Task<T> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync();
        Task UpdatdeAsync(T obj);
        //Task FirstOrDefaultAsync(Func<object, bool> value);
    }
}
