using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using E_Commerce.DataContext;

namespace E_Commerce.DataContext
{
    public class EcommerceDbContextFactory : IDesignTimeDbContextFactory<EcommerceDbContext>
    {
        public EcommerceDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("EcommerceConnectionString")
                ?? "Server=(localdb)\\MSSQLLocalDB;Database=Ecommerce;Trusted_Connection=True;TrustServerCertificate=True;";

            var optionsBuilder = new DbContextOptionsBuilder<EcommerceDbContext>();
            optionsBuilder.UseSqlServer(connectionString, sql => sql.CommandTimeout(180));

            return new EcommerceDbContext(optionsBuilder.Options);
        }
    }
}