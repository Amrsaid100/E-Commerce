using FluentValidation;
using FluentValidation.AspNetCore;
using E_Commerce.DataContext;
using E_Commerce.Repository;
using E_Commerce.UnitOfWork;
using E_Commerce.Services.ProductService;
using E_Commerce.Services.CategoryService;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // FluentValidation
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            // Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Database Context
            builder.Services.AddDbContext<EcommerceDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("EcommerceConnectionString"));
            });

            // Repositories
            builder.Services.AddScoped(typeof(IGenericRepo<>), typeof(GenericRepo<>));
            builder.Services.AddScoped<IProductRepo, ProductRepo>();
            builder.Services.AddScoped<ICartRepo, CartRepo>();

            // Unit of Work
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

            // Services
            builder.Services.AddScoped<IProductService, ProdService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
