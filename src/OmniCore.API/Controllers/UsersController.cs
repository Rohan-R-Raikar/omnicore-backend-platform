using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace OmniCore.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var fullName = User.FindFirstValue("fullName");
            var role = User.FindFirstValue(ClaimTypes.Role);

            var permissions = User.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToList();

            return Ok(new
            {
                userId,
                email,
                fullName,
                role,
                permissions
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnly()
        {
            return Ok("Only Admin role can access this endpoint");
        }

        [HttpGet("check-permission/{permission}")]
        public IActionResult CheckPermission(string permission)
        {
            var permissions = User.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToList();

            if (!permissions.Contains(permission))
            {
                return Forbid();
            }

            return Ok(new
            {
                message = "Permission granted",
                permissionChecked = permission
            });
        }
    }
}