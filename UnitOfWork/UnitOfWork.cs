using E_Commerce.DataContext;
using E_Commerce.Entities;
using E_Commerce.Repository;
using System.Drawing;

namespace E_Commerce.UnitOfWork
{
    public class UnitOfWork:IUnitOfWork
    {
        private readonly EcommerceDbContext context;

        private IGenericRepo<Cart> _Cart;
        private IGenericRepo<User> _User;
        private IGenericRepo<Product> _Product;
        private IGenericRepo<Order> _Order;
        private IGenericRepo<Category> _Category;
        private IGenericRepo<CartItem> _CartItem;
        private IGenericRepo<ProductImage> _ProductImage;
        private IGenericRepo<ProductVariant> _ProductVariant;
        private IGenericRepo<OrderItem> _OrderItem;
        
        public UnitOfWork(EcommerceDbContext context)
        {
            this.context = context;
        }

        public IGenericRepo<Cart> Carts
        {
            get
            {
                if (_Cart == null)
                    _Cart = new GenericRepo<Cart>(context);
                return _Cart;
            }
        }
        public IGenericRepo<CartItem> CartItems 
        {
            get
            {
                if (_CartItem == null)
                    _CartItem = new GenericRepo<CartItem>(context);
                return _CartItem;
            }
        }
        public IGenericRepo<Category> Categories {
            get
            {
                if (_Category == null)
                    _Category = new GenericRepo<Category>(context);
                return _Category;
            }
        }
        public IGenericRepo<Order> Orders {
            get
            {
                if (_Order == null)
                    _Order = new GenericRepo<Order>(context);
                return _Order;
            }
        }
        public IGenericRepo<OrderItem> OrderItems 
        {
            get
            {
                if (_OrderItem == null)
                    _OrderItem = new GenericRepo<OrderItem>(context);
                return _OrderItem;
            }
        }
        public IGenericRepo<Product> Products 
        {
            get
            {
                if (_Product == null)
                    _Product = new GenericRepo<Product>(context);
                return _Product;
            }
        }
        public IGenericRepo<ProductImage> productImage 
        {
            get
            {
                if (_ProductImage == null)
                    _ProductImage = new GenericRepo<ProductImage>(context);
                return _ProductImage;
            }
        }
        public IGenericRepo<ProductVariant> ProductVariants 
        {
            get
            {
                if (_ProductVariant == null)
                    _ProductVariant = new GenericRepo<ProductVariant>(context);
                return _ProductVariant;
            }
        }
        public IGenericRepo<User> Users 
        {
            get
            {
                if (_User == null)
                    _User = new GenericRepo<User>(context);
                return _User;
            }
        }

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }
    }
}
