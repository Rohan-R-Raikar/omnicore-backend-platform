using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmniCore.Application.DTOs.Auth;
using OmniCore.Application.Interfaces;
using OmniCore.Domain.Entities;
using OmniCore.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtTokenGenerator _jwt;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IJwtTokenGenerator jwt, IConfiguration configuration)
        {
            _context = context;
            _jwt = jwt;
            _configuration = configuration;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var userExists = await _context.Users
                .AnyAsync(x => x.Email == request.Email);

            if (userExists)
                throw new Exception("User already exists");

            var user = new User
            {
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333")
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var role = await _context.Roles
                                .Include(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                                .FirstOrDefaultAsync(r => r.Id == user.RoleId);

            if (role == null)
                throw new Exception("Assigned role does not exist");

            var roles = new List<string> { role.Name };
            var permissions = role.RolePermissions
                                  .Select(rp => rp.Permission?.Code ?? string.Empty)
                                  .Where(p => !string.IsNullOrEmpty(p))
                                  .ToList();
            var accessToken = _jwt.GenerateToken(user, roles, permissions);
            var refreshToken = await CreateRefreshToken(user.Id);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = GetAccessTokenExpiry()
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
        {
            var user = await _context.Users
                                    .Include(u => u.Role)
                                    .ThenInclude(r => r.RolePermissions)
                                    .ThenInclude(rp => rp.Permission)
                                    .FirstOrDefaultAsync(x => x.Email == request.Email, ct);

            if (user == null)
                throw new Exception("Invalid credentials");

            var validPassword = BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash
            );

            if (!validPassword)
                throw new Exception("Invalid credentials");

            var existingTokens = _context.RefreshTokens
                .Where(x => x.UserId == user.Id);

            _context.RefreshTokens.RemoveRange(existingTokens);
            await _context.SaveChangesAsync(ct);

            if (user.Role == null)
                throw new Exception("User has no role assigned");

            var roles = new List<string> { user.Role.Name };
            var permissions = user.Role.RolePermissions
                                  .Select(rp => rp.Permission?.Code ?? string.Empty)
                                  .Where(p => !string.IsNullOrEmpty(p))
                                  .ToList();
            var accessToken = _jwt.GenerateToken(user, roles, permissions);
            var refreshToken = await CreateRefreshToken(user.Id);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = GetAccessTokenExpiry()
            };
        }
        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .Include(x => x.User)
                .ThenInclude(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(x => x.Token == refreshToken);

            if (token == null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
                throw new Exception("Invalid refresh token");

            if (token.User == null)
                throw new Exception("User not found for this refresh token");

            var role = token.User.Role;

            if (role == null)
                throw new Exception("User has no role assigned");

            var roles = new List<string> { role.Name };
            var permissions = role.RolePermissions.Select(rp => rp.Permission?.Code ?? string.Empty)
                                                  .Where(p => !string.IsNullOrEmpty(p))
                                                  .ToList();

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;

            var accessToken = _jwt.GenerateToken(token.User, roles, permissions);

            var newRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var refreshTokenEntity = new RefreshToken
            {
                UserId = token.UserId,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = GetAccessTokenExpiry()
            };
        }
        public async Task LogoutAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == refreshToken);

            if (token == null)
                throw new Exception("Invalid refresh token");

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private async Task<string> CreateRefreshToken(Guid userId)
        {
            var token = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(64)
            );

            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                RevokedAt = null
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return token;
        }
        private DateTime GetAccessTokenExpiry()
        {
            if (!double.TryParse(_configuration["Jwt:ExpiryMinutes"], out var minutes))
                minutes = 60; 

            return DateTime.UtcNow.AddMinutes(minutes);
        }
    }
}
