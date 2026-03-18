using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniCore.Application.DTOs.Common;
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

            var result = new
            {
                userId,
                email,
                fullName,
                role,
                permissions
            };

            return Ok(new ApiResponse<object>(result));
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

            var result = new
            {
                message = "Permission granted",
                permissionChecked = permission
            };

            return Ok(new ApiResponse<object>(result));
        }
    }
}