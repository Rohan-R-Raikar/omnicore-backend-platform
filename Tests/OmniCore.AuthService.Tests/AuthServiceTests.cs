using Microsoft.Extensions.Logging;
using Moq;
using OmniCore.AuthService.Models.DTOs;
using OmniCore.AuthService.Models.Entities;
using OmniCore.AuthService.Models.Enums;
using OmniCore.AuthService.Repositories.Interfaces;
using OmniCore.AuthService.Security;
using OmniCore.AuthService.Services.Implementations;

using AuthServiceImpl = OmniCore.AuthService.Services.Implementations.AuthService;
namespace OmniCore.AuthService.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IPasswordHasher> _passwordHasher;
    private readonly Mock<IJwtTokenService> _jwtTokenService;
    private readonly Mock<ILogger<AuthServiceImpl>> _logger;

    private readonly AuthServiceImpl _authService;

    public AuthServiceTests()
    {
        _userRepository = new Mock<IUserRepository>();
        _passwordHasher = new Mock<IPasswordHasher>();
        _jwtTokenService = new Mock<IJwtTokenService>();
        _logger = new Mock<ILogger<AuthServiceImpl>>();

        _authService = new AuthServiceImpl(
            _userRepository.Object,
            _passwordHasher.Object,
            _jwtTokenService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_CreatesCustomer()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Rohan",
            Email = "rohan@example.com",
            Password = "Test@123"
        };

        _userRepository
            .Setup(repository => repository.EmailExistsAsync(
                "rohan@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasher
            .Setup(hasher => hasher.Hash(request.Password))
            .Returns("hashed-password");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.Equal("Rohan", result.Name);
        Assert.Equal("rohan@example.com", result.Email);
        Assert.Equal("Customer", result.Role);

        _userRepository.Verify(
            repository => repository.AddAsync(
                It.Is<User>(user =>
                    user.Email == "rohan@example.com" &&
                    user.Role == UserRole.Customer &&
                    user.PasswordHash == "hashed-password"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _userRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Rohan",
            Email = "rohan@example.com",
            Password = "Test@123"
        };

        _userRepository
            .Setup(repository => repository.EmailExistsAsync(
                "rohan@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act + Assert
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authService.RegisterAsync(request));

        Assert.Equal(
            "A user with this email already exists.",
            exception.Message);

        _userRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "rohan@example.com",
            Password = "Test@123"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Rohan",
            Email = "rohan@example.com",
            PasswordHash = "hashed-password",
            Role = UserRole.Customer
        };

        _userRepository
            .Setup(repository => repository.GetByEmailAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasher
            .Setup(hasher => hasher.Verify(
                request.Password,
                user.PasswordHash))
            .Returns(true);

        var expiresAt = DateTime.UtcNow.AddHours(1);

        _jwtTokenService
            .Setup(service => service.GenerateToken(
                user,
                out expiresAt))
            .Returns("test-token");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-token", result.Token);
        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal("Customer", result.User.Role);
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "missing@example.com",
            Password = "Test@123"
        };

        _userRepository
            .Setup(repository => repository.GetByEmailAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.Null(result);

        _passwordHasher.Verify(
            hasher => hasher.Verify(
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "rohan@example.com",
            Password = "WrongPassword"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Rohan",
            Email = "rohan@example.com",
            PasswordHash = "hashed-password",
            Role = UserRole.Customer
        };

        _userRepository
            .Setup(repository => repository.GetByEmailAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasher
            .Setup(hasher => hasher.Verify(
                request.Password,
                user.PasswordHash))
            .Returns(false);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.Null(result);

        _jwtTokenService.Verify(
            service => service.GenerateToken(
                It.IsAny<User>(),
                out It.Ref<DateTime>.IsAny),
            Times.Never);
    }
}