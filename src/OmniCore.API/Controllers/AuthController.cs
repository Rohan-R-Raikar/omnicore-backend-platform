using Microsoft.AspNetCore.Mvc;
using OmniCore.Application.DTOs.Auth;
using OmniCore.Application.Interfaces;

namespace OmniCore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            try
            {
                var token = await _authService.RegisterAsync(request);

                return Ok(new
                {
                    message = "User registered successfully",
                    token
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            try
            {
                var token = await _authService.LoginAsync(request);

                return Ok(new
                {
                    message = "Login successful",
                    token
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new
                {
                    error = ex.Message
                });
            }
        }
    }
}
