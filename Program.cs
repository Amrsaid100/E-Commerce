using E_Commerce.DataContext;
using E_Commerce.Repository;
using E_Commerce.Services.Authservice;
using E_Commerce.Services.CategoryService;
using E_Commerce.Services.JwtServices;
using E_Commerce.Services.PayMob;
using E_Commerce.Services.ProductService;
using E_Commerce.UnitOfWork;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.Metrics;
using System.Text;

namespace E_Commerce
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Controllers
            builder.Services.AddControllers();

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


            // Unit of Work
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

            // Services
            //after builder.Services.AddAuthorization();

            builder.Services.AddSingleton<IJwtService, E_Commerce.Services.JwtServices.JwtService>();
            builder.Services.AddScoped<IAuthService, E_Commerce.Services.Authservice.AuthService>();
            builder.Services.AddHttpClient<IPaymobService, PaymobService>();


            //  JWT Authentication
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
     });

            builder.Services.AddAuthorization();

            var app = builder.Build();
            await DbSeeder.SeedOwnerAsync(app);

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Important: Authentication Before Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();
        }
    }
}
