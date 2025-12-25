
namespace E_Commerce.Repository
{
    public interface IGenericRepo<T> where T : class
    {
        Task AddAsync(T obj);
        Task DeleteAsync(T obj);
        Task<T> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync();
        Task UpdatdeAsync(T obj);
<<<<<<< HEAD
        
=======
        //Task FirstOrDefaultAsync(Func<object, bool> value);
>>>>>>> a5f80a1ff0b061a1452ff818d8a59430d4260ae8
    }
}
