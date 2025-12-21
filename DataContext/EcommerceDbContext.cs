using E_Commerce.Entities;
using Microsoft.EntityFrameworkCore;


namespace E_Commerce.DataContext
{
    public class EcommerceDbContext :DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<Category> Categories { get; set; }


      
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
        public EcommerceDbContext(DbContextOptions<EcommerceDbContext> options)
    : base(options)
        {
        }
    }
}
