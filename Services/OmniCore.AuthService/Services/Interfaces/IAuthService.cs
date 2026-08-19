using OmniCore.AuthService.Models.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCore.AuthService.Services.Interfaces;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}