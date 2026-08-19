using Microsoft.EntityFrameworkCore;
using OmniCore.AuthService.Models.Entities;

namespace OmniCore.AuthService.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AuthDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}