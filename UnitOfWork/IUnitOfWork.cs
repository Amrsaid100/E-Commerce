using E_Commerce.Entities;
using E_Commerce.Repository;

namespace E_Commerce.UnitOfWork
{
    public interface IUnitOfWork
    {   
        public ICartRepo Carts { get;}
        
        public IGenericRepo<CartItem> CartItems { get; }

        public IGenericRepo<Category> Categories { get; }

        public IGenericRepo<Order> Orders { get; }

        public IGenericRepo<OrderItem> OrderItems { get; }

        public IProductRepo Products { get; }
        public IGenericRepo<ProductImage> productImage { get; }
        public IGenericRepo<ProductVariant> ProductVariants { get;}
        public IGenericRepo<User> Users { get; }
        Task SaveChangesAsync ();
    }
}
