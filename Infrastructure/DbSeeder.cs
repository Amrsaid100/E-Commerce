using System;
using System.Security.Cryptography;
using E_Commerce.Dtos.Roles;
using E_Commerce.Entities;
using E_Commerce.UnitOfWork;
using Microsoft.Extensions.Configuration;

public static class DbSeeder
{
    public static async Task SeedOwnerAsync(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var ownerEmail = config["Owner:Email"];

        if (string.IsNullOrWhiteSpace(ownerEmail))
            throw new Exception("Owner email is not configured");

        var owner = await uow.Users.GetByEmailAsync(ownerEmail);

        if (owner == null)
        {
            // Use real hashed password when available; placeholder prevents NULL insert.
            var placeholderHash = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            await uow.Users.AddAsync(new User
            {
                Email = ownerEmail,
                Name = "System Owner",
                Role = UserRole.Owner,
                PasswordHash = placeholderHash
            });

            await uow.SaveChangesAsync();
        }
    }
}