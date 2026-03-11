using Microsoft.EntityFrameworkCore;
using OmniCore.Application.DTOs.Auth;
using OmniCore.Application.Interfaces;
using OmniCore.Domain.Entities;
using OmniCore.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtTokenGenerator _jwt;

        public AuthService(ApplicationDbContext context, IJwtTokenGenerator jwt)
        {
            _context = context;
            _jwt = jwt;
        }

        public async Task<string> RegisterAsync(RegisterRequest request)
        {
            var userExists = await _context.Users
                .AnyAsync(x => x.Email == request.Email);

            if (userExists)
                throw new Exception("User already exists");

            var user = new User
            {
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return _jwt.GenerateToken(user);
        }

        public async Task<string> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == request.Email);

            if (user == null)
                throw new Exception("Invalid credentials");

            var validPassword = BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash
            );

            if (!validPassword)
                throw new Exception("Invalid credentials");

            return _jwt.GenerateToken(user);
        }
    }
}
