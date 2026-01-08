using E_Commerce.DataContext;
using E_Commerce.Middleware;
using E_Commerce.Repositories.CategoryRepository;
using E_Commerce.Repository;
using E_Commerce.Services.Authservice;
using E_Commerce.Services.CartService;
using E_Commerce.Services.CategoryService;
using E_Commerce.Services.EmailService;
using E_Commerce.Services.JwtServices;
using E_Commerce.Services.PayMob;
using E_Commerce.Services.PaymentService;
using E_Commerce.Services.ProductService;
using E_Commerce.UnitOfWork;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
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
                    builder.Configuration.GetConnectionString("EcommerceConnectionString"));
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
            builder.Services.AddHttpClient<IPaymobService, PaymobService>();
            builder.Services.AddScoped<IProductService, ProdService>();
            builder.Services.AddScoped<ICartService, CartServices>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddHttpClient<IPaymentService, PaymentSer>();

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
                    policy.WithOrigins("http://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // Rate Limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Request.Headers["Authorization"].ToString(),
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

            // Ensure DB schema is up-to-date before seeding
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceDbContext>();
                db.Database.Migrate();
            }

            await DbSeeder.SeedOwnerAsync(app);

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Middleware Pipeline
            app.UseMiddleware<RequestResponseLoggingMiddleware>();
            app.UseMiddleware<GlobalExceptionMiddleware>();

            app.UseHttpsRedirection();

            // CORS must be before Auth
            app.UseCors("AllowAngular");

            app.UseRateLimiter();

            // Important: Authentication Before Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();
        }
    }
}