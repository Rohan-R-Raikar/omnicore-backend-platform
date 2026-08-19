using Microsoft.EntityFrameworkCore;
using OmniCore.AuthService.Models.Entities;
using OmniCore.AuthService.Models.Enums;
using OmniCore.AuthService.Security;
using System;
using System.Threading.Tasks;

namespace OmniCore.AuthService.Data;

public static class AuthDbSeeder
{
    public static async Task SeedAsync(
        AuthDbContext context,
        IPasswordHasher passwordHasher)
    {
        const string adminEmail = "admin@omnicore.com";

        var adminExists = await context.Users
            .AnyAsync(user => user.Email == adminEmail);

        if (adminExists)
        {
            return;
        }

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Name = "OmniCore Admin",
            Email = adminEmail,
            PasswordHash = passwordHasher.Hash("Admin@123"),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddAsync(admin);
        await context.SaveChangesAsync();
    }
}