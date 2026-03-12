using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace OmniCore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var fullName = User.FindFirstValue("fullName");
            var role = User.FindFirstValue(ClaimTypes.Role);

            return Ok(new
            {
                userId,
                email,
                fullName,
                role
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("all-users")]
        public IActionResult GetAllUsers()
        {
            return Ok("Only admins can see this");
        }
    }
}