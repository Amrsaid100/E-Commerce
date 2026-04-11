using E_Commerce.DataContext;
using E_Commerce.Helpers;
using E_Commerce.Middleware;
using E_Commerce.Repositories.CategoryRepository;
using E_Commerce.Repository;
using E_Commerce.Services.Authservice;
using E_Commerce.Services.CartService;
using E_Commerce.Services.CategoryService;
using E_Commerce.Services.EmailService;
using E_Commerce.Services.JwtServices;
using E_Commerce.Services.PayMob;
using E_Commerce.Services.FileStorage;
using E_Commerce.Services.ProductService;
using E_Commerce.Services.ShopSettingsService;
using E_Commerce.UnitOfWork;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
namespace E_Commerce
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Controllers
            builder.Services.AddControllers().AddJsonOptions(options => {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            // FluentValidation
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // DbContext
            builder.Services.AddDbContext<EcommerceDbContext>(options =>
            {
                options.UseSqlServer(
      builder.Configuration.GetConnectionString("EcommerceConnectionString"),
      sqlOptions => sqlOptions.CommandTimeout(180) 
  );
            });

            // Repositories
            builder.Services.AddScoped(typeof(IGenericRepo<>), typeof(GenericRepo<>));
            builder.Services.AddScoped<IProductRepo, ProductRepo>();
            builder.Services.AddScoped<ICartRepo, CartRepo>();
            builder.Services.AddScoped<IUserRepo, UserRepo>();
            builder.Services.AddScoped<IRefreshTokenRepo, RefreshTokenRepo>();
            builder.Services.AddScoped<IRevokedTokenRepo, RevokedTokenRepo>();
            builder.Services.AddScoped<IOrderRepo, OrderRepo>();
            builder.Services.AddScoped<ICategoryRepo, CategoryRepo>();


            // Unit of Work
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

            // Services
            builder.Services.AddSingleton<IJwtService, E_Commerce.Services.JwtServices.JwtService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IAuthService, E_Commerce.Services.Authservice.AuthService>();
            builder.Services.AddScoped<IProductService, ProdService>();
            builder.Services.AddScoped<ICartService, CartServices>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();

            // Shop settings & file storage
            builder.Services.AddScoped<IShopSettingsService, ShopSettingsService>();
            builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

            // Paymob payment services
            builder.Services.Configure<PaymobSettings>(builder.Configuration.GetSection("Paymob"));
            builder.Services.AddHttpClient<IPaymobClient, PaymobClient>();
            builder.Services.AddScoped<IPaymobPaymentService, PaymobPaymentService>();
            builder.Services.AddHostedService<PaymentTimeoutService>();

            // Clear default inbound claim type map to prevent automatic claim renaming
            System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            // JWT Authentication
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
         var issuer = builder.Configuration["Jwt:Issuer"];
         var audience = builder.Configuration["Jwt:Audience"];
         var key = builder.Configuration["Jwt:Key"];

         options.TokenValidationParameters = new TokenValidationParameters
         {
             ValidateIssuer = true, 
             ValidateAudience = true,
             ValidateLifetime = true,
             ValidateIssuerSigningKey = true,
             RequireExpirationTime = true,
             ClockSkew = TimeSpan.FromMinutes(2),

             ValidIssuer = issuer,
             ValidAudience = audience,
             IssuerSigningKey = new SymmetricSecurityKey(
                 Encoding.UTF8.GetBytes(key!)
             )
         };

                    //  IMPORTANT: check revoked jti on every request
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"❌ Auth Failed: {context.Exception.Message}");
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = async context =>
                        {
                            Console.WriteLine($"✅ Token Validated - User: {context.Principal?.Identity?.Name}");

                            var db = context.HttpContext.RequestServices
                                .GetRequiredService<EcommerceDbContext>();

                            var jti = context.Principal?
                                .FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)
                                ?.Value;

                            Console.WriteLine($"🔍 Checking JTI: {jti}");

                            if (!string.IsNullOrEmpty(jti))
                            {
                                var revoked = await db.RevokedTokens
                                    .AnyAsync(x => x.Jti == jti && x.ExpiresAtUtc > DateTime.UtcNow);

                                Console.WriteLine($"🔍 Token revoked? {revoked}");

                                if (revoked)
                                {
                                    Console.WriteLine($"❌ Token is REVOKED!");
                                    context.Fail("Token revoked");
                                }
                            }
                        }
                    };
                });

            builder.Services.AddAuthorization();

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngular", policy => {
                    policy.AllowAnyOrigin()  // Allow all origins for development
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // Rate Limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Request.Headers["Authorization"].ToString() ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",

                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Too many requests. Please try again later." }, cancellationToken: token);
                };
            });

            var app = builder.Build();

            // Validate Paymob configuration — fails fast in Production if env vars are missing
            PaymobConfigValidator.Validate(
                app.Services,
                app.Environment,
                app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("PaymobConfigValidator"));

            // Ensure DB schema is up-to-date before seeding
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceDbContext>();
                
                if (app.Environment.IsDevelopment())
                {
                    try
                    {
                        // Dev only: make sure DB exists + migrations applied (Migrate is idempotent)
                        Console.WriteLine(" Ensuring database is migrated...");
                        db.Database.Migrate();
                        Console.WriteLine(" Database ready");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($" Migration failed: {ex.Message}");
                        // لا ترمي Exception في Production
                    }
                }
            }

            try
            {
                await DbSeeder.SeedOwnerAsync(app);
            }
            catch (Exception ex)
            {
                // Don't crash the whole app if DB is misconfigured/unavailable
                Console.WriteLine($"❌ Seeding owner failed: {ex.Message}");
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Static files (wwwroot - add product form, etc.)
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Middleware Pipeline
            app.UseMiddleware<RequestResponseLoggingMiddleware>();
            app.UseMiddleware<GlobalExceptionMiddleware>();

            // CORS must be before Auth
            app.UseCors("AllowAngular");

            // Temporarily disable Rate Limiter for debugging
            // app.UseRateLimiter();

            // Important: Authentication Before Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
                app.Run("http://localhost:7116");
        }
    }
}