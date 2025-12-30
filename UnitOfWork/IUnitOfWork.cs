using E_Commerce.Entities;
using E_Commerce.Repositories.CategoryRepository;
using E_Commerce.Repository;

namespace E_Commerce.UnitOfWork
{
    public interface IUnitOfWork
    {
        public ICartRepo Carts { get; }

        public IGenericRepo<CartItem> CartItems { get; }

        ICategoryRepo Categories { get; }

        public IOrderRepo Orders { get; }

        public IGenericRepo<OrderItem> OrderItems { get; }

        public IProductRepo Products { get; }
       
        public IGenericRepo<ProductImage> ProductImages { get; }

        public IGenericRepo<ProductVariant> ProductVariants { get; }
        IUserRepo Users { get; }


        Task SaveChangesAsync();
    }
}
