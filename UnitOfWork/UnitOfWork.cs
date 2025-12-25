using E_Commerce.DataContext;
using E_Commerce.Entities;
using E_Commerce.Repository;
using E_Commerce.Repositories.CategoryRepository;

namespace E_Commerce.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly EcommerceDbContext context;

        private readonly Lazy<ICartRepo> _Cart;
        private readonly Lazy<IUserRepo> _User;               
        private readonly Lazy<IProductRepo> _Product;
        private readonly Lazy<IGenericRepo<Order>> _Order;
        private readonly Lazy<ICategoryRepo> _Category;
        private readonly Lazy<IGenericRepo<CartItem>> _CartItem;
        private readonly Lazy<IGenericRepo<ProductImage>> _ProductImage;
        private readonly Lazy<IGenericRepo<ProductVariant>> _ProductVariant;
        private readonly Lazy<IGenericRepo<OrderItem>> _OrderItem;

        public UnitOfWork(EcommerceDbContext context)
        {
            this.context = context;

            _Cart = new Lazy<ICartRepo>(() => new CartRepo(context));
            _User = new Lazy<IUserRepo>(() => new UserRepo(context));        
            _Product = new Lazy<IProductRepo>(() => new ProductRepo(context));
            _Order = new Lazy<IGenericRepo<Order>>(() => new GenericRepo<Order>(context));
            _Category = new Lazy<ICategoryRepo>(() => new CategoryRepo(context));
            _CartItem = new Lazy<IGenericRepo<CartItem>>(() => new GenericRepo<CartItem>(context));
            _ProductImage = new Lazy<IGenericRepo<ProductImage>>(() => new GenericRepo<ProductImage>(context));
            _ProductVariant = new Lazy<IGenericRepo<ProductVariant>>(() => new GenericRepo<ProductVariant>(context));
            _OrderItem = new Lazy<IGenericRepo<OrderItem>>(() => new GenericRepo<OrderItem>(context));
        }

        public ICartRepo Carts => _Cart.Value;
        public IGenericRepo<CartItem> CartItems => _CartItem.Value;
        public ICategoryRepo Categories => _Category.Value;
        public IGenericRepo<Order> Orders => _Order.Value;
        public IGenericRepo<OrderItem> OrderItems => _OrderItem.Value;
        public IProductRepo Products => _Product.Value;
        public IGenericRepo<ProductImage> ProductImages => _ProductImage.Value;
        public IGenericRepo<ProductVariant> ProductVariants => _ProductVariant.Value;

        public IUserRepo Users => _User.Value;                  

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }
    }

}
