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
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<RevokedToken> RevokedTokens { get; set; }
        public DbSet<Governorate> Governorates { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentAttempt> PaymentAttempts { get; set; }
        public DbSet<PaymentAuditLog> PaymentAuditLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Cart>()
                .Property(x => x.TotalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CartItem>()
                .Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(x => x.UnitePrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(x => x.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ProductVariant>()
                .Property(x => x.Price)
                .HasPrecision(18, 2);

            // Payment configuration - prevent cascade delete conflict
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany()
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payment indexes
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.IdempotencyKey)
                .IsUnique();

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.ProviderOrderId);

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.ProviderTransactionId);

            // PaymentAuditLog indexes
            modelBuilder.Entity<PaymentAuditLog>()
                .HasIndex(a => a.PaymentId);

            modelBuilder.Entity<PaymentAuditLog>()
                .HasIndex(a => a.Timestamp);

            modelBuilder.Entity<Governorate>().HasData(
       new Governorate { Id = 1, NameAr = "القاهرة", NameEn = "Cairo", ShippingCost = 50.00m },
       new Governorate { Id = 2, NameAr = "الجيزة", NameEn = "Giza", ShippingCost = 50.00m },
       new Governorate { Id = 3, NameAr = "الإسكندرية", NameEn = "Alexandria", ShippingCost = 60.00m },
       new Governorate { Id = 4, NameAr = "الدقهلية", NameEn = "Dakahlia", ShippingCost = 70.00m },
       new Governorate { Id = 5, NameAr = "البحر الأحمر", NameEn = "Red Sea", ShippingCost = 100.00m },
       new Governorate { Id = 6, NameAr = "البحيرة", NameEn = "Beheira", ShippingCost = 70.00m },
       new Governorate { Id = 7, NameAr = "الفيوم", NameEn = "Fayoum", ShippingCost = 60.00m },
       new Governorate { Id = 8, NameAr = "الغربية", NameEn = "Gharbia", ShippingCost = 65.00m },
       new Governorate { Id = 9, NameAr = "الإسماعيلية", NameEn = "Ismailia", ShippingCost = 75.00m },
       new Governorate { Id = 10, NameAr = "المنوفية", NameEn = "Monufia", ShippingCost = 60.00m },
       new Governorate { Id = 11, NameAr = "المنيا", NameEn = "Minya", ShippingCost = 80.00m },
       new Governorate { Id = 12, NameAr = "القليوبية", NameEn = "Qalyubia", ShippingCost = 55.00m },
       new Governorate { Id = 13, NameAr = "الوادي الجديد", NameEn = "New Valley", ShippingCost = 120.00m },
       new Governorate { Id = 14, NameAr = "الشرقية", NameEn = "Sharqia", ShippingCost = 65.00m },
       new Governorate { Id = 15, NameAr = "سوهاج", NameEn = "Sohag", ShippingCost = 90.00m },
       new Governorate { Id = 16, NameAr = "جنوب سيناء", NameEn = "South Sinai", ShippingCost = 110.00m },
       new Governorate { Id = 17, NameAr = "كفر الشيخ", NameEn = "Kafr El Sheikh", ShippingCost = 70.00m },
       new Governorate { Id = 18, NameAr = "مطروح", NameEn = "Matrouh", ShippingCost = 100.00m },
       new Governorate { Id = 19, NameAr = "الأقصر", NameEn = "Luxor", ShippingCost = 95.00m },
       new Governorate { Id = 20, NameAr = "قنا", NameEn = "Qena", ShippingCost = 90.00m },
       new Governorate { Id = 21, NameAr = "أسوان", NameEn = "Aswan", ShippingCost = 100.00m },
       new Governorate { Id = 22, NameAr = "أسيوط", NameEn = "Asyut", ShippingCost = 85.00m },
       new Governorate { Id = 23, NameAr = "بني سويف", NameEn = "Beni Suef", ShippingCost = 70.00m },
       new Governorate { Id = 24, NameAr = "بورسعيد", NameEn = "Port Said", ShippingCost = 75.00m },
       new Governorate { Id = 25, NameAr = "دمياط", NameEn = "Damietta", ShippingCost = 75.00m },
       new Governorate { Id = 26, NameAr = "شمال سيناء", NameEn = "North Sinai", ShippingCost = 110.00m },
       new Governorate { Id = 27, NameAr = "السويس", NameEn = "Suez", ShippingCost = 70.00m }
   );
        }

        public EcommerceDbContext(DbContextOptions<EcommerceDbContext> options)
    : base(options)
        {
        }
    }
}
