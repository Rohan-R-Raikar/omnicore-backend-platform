using Microsoft.EntityFrameworkCore;
using OmniCore.AuthService.Models.Entities;
using OmniCore.AuthService.Models.Enums;
using OmniCore.AuthService.Security;

namespace OmniCore.AuthService.Data;

public static class AuthDbSeeder
{
    public static async Task SeedAsync(
        AuthDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        var adminName =
            configuration["SeedAdmin:Name"]
            ?? throw new InvalidOperationException(
                "SeedAdmin:Name is not configured.");

        var adminEmail =
            configuration["SeedAdmin:Email"]
            ?? throw new InvalidOperationException(
                "SeedAdmin:Email is not configured.");

        var adminPassword =
            configuration["SeedAdmin:Password"]
            ?? throw new InvalidOperationException(
                "SeedAdmin:Password is not configured.");

        adminEmail = adminEmail
            .Trim()
            .ToLowerInvariant();

        var adminExists = await context.Users
            .AnyAsync(user => user.Email == adminEmail);

        if (adminExists)
        {
            return;
        }

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Name = adminName.Trim(),
            Email = adminEmail,
            PasswordHash = passwordHasher.Hash(adminPassword),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddAsync(admin);
        await context.SaveChangesAsync();
    }
}