using Microsoft.Extensions.DependencyInjection;
using OmniCore.Application.Interfaces;
using OmniCore.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCore.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            return services;
        }
    }
}
