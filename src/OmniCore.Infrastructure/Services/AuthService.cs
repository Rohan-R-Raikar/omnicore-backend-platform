using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext context, 
            IJwtTokenGenerator jwt, 
            IConfiguration configuration, 
            ILogger<AuthService> logger)
        {
            _context = context;
            _jwt = jwt;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Register attempt for {Email}", request.Email);
                var userExists = await _context.Users
                .AnyAsync(x => x.Email == request.Email);

                if (userExists)
                {
                    _logger.LogWarning("Registration failed. User already exists {Email}", request.Email);

                    throw new Exception("User already exists");
                }
                var user = new User
                {
                    Email = request.Email,
                    FullName = request.FullName,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333")
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User created {UserId}", user.Id);

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

                _logger.LogInformation("Registration successful for {UserId}", user.Id);

                return new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = GetAccessTokenExpiry()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {Email}", request.Email);
                throw;
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
        {
            try 
            { 
                _logger.LogInformation("Login attempt for {Email}", request.Email);
                var user = await _context.Users
                                        .Include(u => u.Role)
                                        .ThenInclude(r => r.RolePermissions)
                                        .ThenInclude(rp => rp.Permission)
                                        .FirstOrDefaultAsync(x => x.Email == request.Email, ct);

                if (user == null)
                {
                    _logger.LogWarning("Login failed. User not found {Email}", request.Email);
                    throw new Exception("Invalid credentials");
                }

                var validPassword = BCrypt.Net.BCrypt.Verify(
                    request.Password,
                    user.PasswordHash
                );

                if (!validPassword)
                {
                    _logger.LogWarning("Login failed. Invalid password for {Email}", request.Email);
                    throw new Exception("Invalid credentials");
                }

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

                _logger.LogInformation("Login successful for {UserId}", user.Id);
                return new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = GetAccessTokenExpiry()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", request.Email);
                throw;
            }
        }
        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation("Refreshing token");
                var token = await _context.RefreshTokens
                .Include(x => x.User)
                .ThenInclude(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(x => x.Token == refreshToken);

                if (token == null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning("Invalid refresh token used");
                    throw new Exception("Invalid refresh token");
                }

                if (token.User == null)
                {
                    throw new Exception("User not found for this refresh token");
                }

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

                _logger.LogInformation("Token refreshed for {UserId}", token.UserId);

                return new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = GetAccessTokenExpiry()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token failed");
                throw;
            }
        }
        public async Task LogoutAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation("Logout attempt");
                var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == refreshToken);

                if(token == null)
                {
                    _logger.LogWarning("Invalid refresh token during logout");
                    throw new Exception("Invalid refresh token");
                }

                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("User logged out successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                throw;
            }
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
