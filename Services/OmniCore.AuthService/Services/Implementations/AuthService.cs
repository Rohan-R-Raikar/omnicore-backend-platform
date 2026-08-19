using OmniCore.AuthService.Models.DTOs;
using OmniCore.AuthService.Models.Entities;
using OmniCore.AuthService.Models.Enums;
using OmniCore.AuthService.Repositories.Interfaces;
using OmniCore.AuthService.Security;
using OmniCore.AuthService.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCore.AuthService.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<UserResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _userRepository.EmailExistsAsync(
            normalizedEmail,
            cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "A user with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Customer,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);

        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User registered successfully. UserId: {UserId}",
            user.Id);

        return MapUser(user);
    }

    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _userRepository.GetByEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (user is null)
        {
            _logger.LogWarning(
                "Failed login attempt for email {Email}",
                normalizedEmail);

            return null;
        }

        var validPassword = _passwordHasher.Verify(
            request.Password,
            user.PasswordHash);

        if (!validPassword)
        {
            _logger.LogWarning(
                "Failed login attempt for UserId {UserId}",
                user.Id);

            return null;
        }

        var token = _jwtTokenService.GenerateToken(
            user,
            out var expiresAt);

        _logger.LogInformation(
            "User logged in successfully. UserId: {UserId}",
            user.Id);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = MapUser(user)
        };
    }

    private static UserResponse MapUser(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }
}