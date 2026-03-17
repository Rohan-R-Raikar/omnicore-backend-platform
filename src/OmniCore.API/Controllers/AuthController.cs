using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OmniCore.Application.DTOs.Auth;
using OmniCore.Application.Interfaces;

namespace OmniCore.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // Strict (prevent bot registrations)
        [HttpPost("register")]
        [EnableRateLimiting("authPolicy")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }

        // VERY STRICT (brute force protection)
        [HttpPost("login")]
        [EnableRateLimiting("authPolicy")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            var result = await _authService.LoginAsync(request, ct);
            return Ok(result);
        }

        // Moderate
        [HttpPost("refresh")]
        [EnableRateLimiting("relaxedPolicy")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(result);
        }

        //[HttpPost("logout")]
        //public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        //{
        //    await _authService.LogoutAsync(request.RefreshToken);
        //    return NoContent();
        //}

        // Relaxed
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [EnableRateLimiting("relaxedPolicy")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            await _authService.LogoutAsync(request.RefreshToken);
            return NoContent();
        }
    }
}