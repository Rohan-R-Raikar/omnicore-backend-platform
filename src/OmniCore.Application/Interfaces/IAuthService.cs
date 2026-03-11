using OmniCore.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Application.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterRequest request);
        Task<string> LoginAsync(LoginRequest request);
    }
}
