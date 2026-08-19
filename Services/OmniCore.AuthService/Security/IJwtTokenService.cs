using OmniCore.AuthService.Models.Entities;
using System;

namespace OmniCore.AuthService.Security;

public interface IJwtTokenService
{
    string GenerateToken(User user, out DateTime expiresAt);
}