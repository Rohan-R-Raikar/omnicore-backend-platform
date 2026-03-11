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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub");

            var email = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue("email");

            var fullName = User.FindFirstValue("fullName");

            return Ok(new
            {
                userId,
                email,
                fullName
            });
        }
    }
}
