# E-Commerce API

**مشروع API متكامل و آمن لموقع إلكتروني تجاري مع نظام الدفع الإلكتروني والمصادقة الآمنة.**

![Status](https://img.shields.io/badge/Status-Production%20Ready-brightgreen)
![License](https://img.shields.io/badge/License-MIT-blue)
![Version](https://img.shields.io/badge/Version-1.0.0-orange)
![Framework](https://img.shields.io/badge/Framework-.NET%208.0-blueviolet)

## 📋 المحتويات

- [الميزات الرئيسية](#-الميزات-الرئيسية)
- [التكنولوجيا](#-التكنولوجيا-المستخدمة)
- [البدء السريع](#-البدء-السريع)
- [API Documentation](#-api-documentation)
- [الهيكل](#-هيكل-المشروع)
- [Security](#-security-best-practices)
- [Troubleshooting](#-troubleshooting)

## ✨ الميزات الرئيسية

- **Authentication & Authorization**
  - JWT Token مع Refresh Token Rotation
  - Token Revocation
  - OTP-based Authentication
  - User Roles (User, Admin, Owner)

- **Security**
  - Global Exception Handling
  - CORS Configuration
  - Rate Limiting (100 requests/minute)
  - Request/Response Logging
  - SQL Injection Prevention

- **Payment Integration**
  - Paymob Payment Gateway
  - Webhook Support
  - Stock Management on Payment
  - Payment Status Tracking

- **Features**
  - Product Management
  - Shopping Cart
  - Order Management
  - Category Management
  - Email Notifications
  - Comprehensive Logging

## 🔧 التكنولوجيا المستخدمة

- **Backend**: ASP.NET Core 8.0
- **Database**: SQL Server
- **Authentication**: JWT
- **Payment**: Paymob
- **Email**: SMTP (Gmail)
- **Validation**: FluentValidation
- **ORM**: Entity Framework Core

## 🚀 البدء السريع

### المتطلبات:
- .NET 8.0 SDK
- SQL Server
- Gmail Account (مع App Password)
- Paymob Account (اختياري للـ Testing)

### التثبيت:

1. **Clone المشروع:**
```bash
git clone https://github.com/yourusername/e-commerce-api.git
cd e-commerce-api
```

2. **حدّث البيانات في appsettings.Development.json:**
```json
{
  "Email": {
    "SmtpUser": "your-email@gmail.com",
    "SmtpPass": "your-app-password"
  },
  "ConnectionStrings": {
    "EcommerceConnectionString": "your-database-connection"
  },
  "Paymob": {
    "ApiKey": "your-paymob-api-key"
  }
}
```

3. **قم بـ Database Migration:**
```bash
dotnet ef database update
```

4. **شغّل السيرفر:**
```bash
dotnet run
```

السيرفر سيعمل على: `https://localhost:5001`

## 📚 API Documentation

### الـ Endpoints الرئيسية:

#### Authentication
- `POST /api/auth/request-otp` - طلب OTP
- `POST /api/auth/verify-otp` - تحقق من OTP
- `POST /api/auth/refresh` - تجديد Token

#### Products
- `GET /api/product` - جميع المنتجات
- `GET /api/product/{id}` - منتج واحد
- `POST /api/product` - إضافة منتج (Admin)

#### Cart
- `GET /api/cart` - عرض السلة
- `POST /api/cart/add` - إضافة للسلة
- `POST /api/cart/remove` - حذف من السلة

#### Orders
- `GET /api/order` - جميع الطلبات (Admin)
- `GET /api/order/user/{userId}` - طلبات المستخدم
- `POST /api/checkout` - إنشاء طلب

#### Payment
- `POST /api/payment/success` - دفع نجح
- `POST /api/payment/fail` - دفع فشل
- `POST /api/payment/webhook` - Webhook من Paymob

## 🔐 Security Best Practices

### Development
- استخدم `appsettings.Development.json` المحلي
- لا ترفع Secrets على GitHub

### Production
- استخدم **Environment Variables** أو **Azure Key Vault**
- فعّل HTTPS دائماً
- استخدم Strong JWT Key (32+ characters)
- جدّد الـ Tokens بانتظام

## 📊 Database Schema

### Tables:
- **Users** - بيانات المستخدمين
- **Products** - المنتجات
- **ProductVariants** - أنواع المنتجات
- **ProductImages** - صور المنتجات
- **Categories** - التصنيفات
- **Cart** - السلات
- **CartItems** - عناصر السلة
- **Orders** - الطلبات
- **OrderItems** - عناصر الطلب
- **RefreshTokens** - Refresh Tokens
- **RevokedTokens** - الـ Tokens الملغاة

## 🧪 Testing

```bash
# Run unit tests
dotnet test

# Run integration tests
dotnet test --filter "Category=Integration"
```

## 📝 Logging

جميع الـ Requests و Responses يتم تسجيلها في الـ Logs:
- Request Method و Path
- Status Code
- Duration
- Request/Response Body

## 🐛 Troubleshooting

### خطأ في Email:
```
Check SmtpUser and SmtpPass (Gmail App Password)
Enable 2FA on Gmail
Generate App Password from myaccount.google.com/apppasswords
```

### خطأ في Database:
```
Update connection string in appsettings.Development.json
Run: dotnet ef database update
```

### خطأ في Payment:
```
تأكد من بيانات Paymob في appsettings.Development.json
استخدم Webhook URL الصحيح
```

## 📦 Environment Variables (Production)

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443
Jwt:Key=your-secret-key
Jwt:Issuer=your-issuer
Jwt:Audience=your-audience
Email:SmtpUser=your-email
Email:SmtpPass=your-password
ConnectionStrings:EcommerceConnectionString=your-db-connection
Paymob:ApiKey=your-api-key
```

## 🤝 Contributing

1. Fork المشروع
2. إنشاء Branch جديد (`git checkout -b feature/amazing-feature`)
3. Commit التغييرات (`git commit -m 'Add amazing feature'`)
4. Push للـ Branch (`git push origin feature/amazing-feature`)
5. افتح Pull Request

## � Project Structure

```
E-Commerce/
├── Controllers/          # API Controllers
│   ├── AuthController.cs
│   ├── ProductController.cs
│   ├── CartController.cs
│   ├── OrderController.cs
│   ├── PaymentController.cs
│   ├── CategoryController.cs
│   ├── UserController.cs
│   └── CheckOutController.cs
│
├── Services/            # Business Logic
│   ├── AuthService/
│   ├── ProductService/
│   ├── CartService/
│   ├── CategoryService/
│   ├── EmailService/
│   ├── JwtServices/
│   ├── PaymentService/
│   └── PayMob/
│
├── Repository/          # Data Access
│   ├── ProductRepo.cs
│   ├── CartRepo.cs
│   ├── UserRepo.cs
│   ├── OrderRepo.cs
│   ├── CategoryRepository.cs
│   └── GenericRepo.cs
│
├── Entities/            # Database Models
│   ├── User.cs
│   ├── Product.cs
│   ├── Cart.cs
│   ├── Order.cs
│   ├── Category.cs
│   └── ...
│
├── Dtos/                # Data Transfer Objects
│   ├── Auth/
│   ├── CartDto/
│   ├── ProductDtos/
│   ├── OrderDto/
│   └── Payment/
│
├── Middleware/          # Custom Middleware
│   ├── GlobalExceptionMiddleware.cs
│   └── RequestResponseLoggingMiddleware.cs
│
├── Migrations/          # Database Migrations
│   ├── InitialCreate.cs
│   ├── AddMoneyPrecision.cs
│   └── ...
│
├── Validators/          # FluentValidation Validators
│   ├── CheckOutDtoValidator.cs
│   └── ...
│
├── UnitOfWork/          # Unit of Work Pattern
│   ├── IUnitOfWork.cs
│   └── UnitOfWork.cs
│
├── DataContext/         # DbContext
│   └── EcommerceDbContext.cs
│
├── Program.cs           # Application Entry Point
├── appsettings.json     # Configuration (Production)
├── appsettings.Development.json  # Configuration (Development - in .gitignore)
├── README.md            # هذا الملف
├── IMPROVEMENTS.md      # شرح التحسينات الأمنية
└── MIGRATION_GUIDE.md   # دليل الهجرة و الاستخدام
```

---

## 🔄 API Flow Example

### User Registration & Login:
```
1. POST /api/auth/request-otp
   ↓
2. User receives OTP via Email
   ↓
3. POST /api/auth/verify-otp
   ↓
4. Response: { accessToken, refreshToken, userId, role }
```

### Shopping & Payment:
```
1. GET /api/product (View Products)
   ↓
2. POST /api/cart/add (Add to Cart)
   ↓
3. POST /api/checkout (Create Order)
   ↓
4. GET payment URL from Response
   ↓
5. User pays via Paymob
   ↓
6. POST /api/payment/webhook (Paymob Callback)
   ↓
7. Order Status: Paid, Stock Reduced, Cart Cleared
```

---

## 📈 Performance Tips

```csharp
// استخدم Caching للـ Products
builder.Services.AddMemoryCache();

// استخدم Async/Await دائماً
public async Task<IActionResult> GetProducts()
{
    var products = await _repo.Products.GetAllAsync();
    return Ok(products);
}

// استخدم Pagination للـ Large Datasets
public async Task<IActionResult> GetOrders(int pageNumber = 1, int pageSize = 10)
{
    var orders = await _repo.Orders
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    return Ok(orders);
}
```

---

## 🚀 Deployment Options

### Azure:
```bash
dotnet publish -c Release
# Deploy to Azure App Service
```

### Docker:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app
COPY . .
RUN dotnet publish -c Release
ENTRYPOINT ["dotnet", "E-Commerce.dll"]
```

### Environment Variables (for Production):
```bash
export ASPNETCORE_ENVIRONMENT=Production
export Jwt__Key=your-long-secret-key
export Jwt__Issuer=your-issuer
export Jwt__Audience=your-audience
export Email__SmtpUser=your-email@gmail.com
export Email__SmtpPass=your-app-password
export ConnectionStrings__EcommerceConnectionString=your-connection-string
```

---

## 📞 Support & Contact

للأسئلة و الاستفسارات:
- **Email**: eldiastymohamed97@gmail.com
- **GitHub Issues**: [Report an issue](../../issues)
- **Documentation**: انظر `IMPROVEMENTS.md` و `MIGRATION_GUIDE.md`

---

## ✅ Checklist للـ Production Deployment

- [ ] تحديث جميع Connection Strings
- [ ] تفعيل HTTPS
- [ ] استخدام Environment Variables للـ Secrets
- [ ] تشغيل Database Migrations
- [ ] تفعيل Logging و Monitoring
- [ ] إعداد Email Service
- [ ] إعداد Payment Gateway (Paymob)
- [ ] تشغيل Security Tests
- [ ] إعداد Backup Strategy
- [ ] مراجعة الأمان الكامل

---

**تم إنشاء المشروع بواسطة:** Mohamed El-Diasty 
**آخر تحديث:** January 4, 2026  
**الترخيص:** MIT License

