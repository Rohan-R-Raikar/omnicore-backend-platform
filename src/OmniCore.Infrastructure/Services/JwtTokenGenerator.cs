using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OmniCore.Application.Interfaces;
using OmniCore.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OmniCore.Infrastructure.Services
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtTokenGenerator> _logger;

        public JwtTokenGenerator(IConfiguration configuration,
            ILogger<JwtTokenGenerator> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string GenerateToken(User user, IList<string> roles, IList<string> permissions)
        {
            try
            {
                _logger.LogInformation("Generating JWT token for user {UserId}", user.Id);
                var jwtSettings = _configuration.GetSection("Jwt");
                Console.WriteLine(jwtSettings["Key"]);
                if (string.IsNullOrEmpty(jwtSettings["Key"]))
                    throw new Exception("JWT Key not loaded");
                var keyValue = jwtSettings["Key"];
                if (string.IsNullOrWhiteSpace(keyValue))
                {
                    _logger.LogError("JWT Key is not configured.");
                    throw new InvalidOperationException("JWT Key is not configured.");
                }
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim("fullName", user.FullName ?? string.Empty)
                };

                if (roles != null)
                {
                    foreach (var role in roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                    }
                }

                if (permissions != null)
                {
                    foreach (var permission in permissions)
                    {
                        claims.Add(new Claim("permission", permission));
                    }
                }

                double expiryMinutes;
                if (!double.TryParse(jwtSettings["ExpiryMinutes"], out expiryMinutes))
                {
                    expiryMinutes = 60;
                }

                var token = new JwtSecurityToken(
                    issuer: jwtSettings["Issuer"],
                    audience: jwtSettings["Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                    signingCredentials: creds
                );

                _logger.LogInformation("JWT token generated successfully for user {UserId}", user.Id);

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user {UserId}", user?.Id);
                throw;
            }
        }
    }
}