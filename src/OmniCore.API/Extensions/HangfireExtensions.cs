using Hangfire;

namespace OmniCore.API.Extensions
{
    public static class HangfireExtensions
    {
        public static IServiceCollection AddHangfireConfig(this IServiceCollection services, IConfiguration config)
        {
            services.AddHangfire(cfg =>
                cfg.UseSqlServerStorage(config.GetConnectionString("DefaultConnection")));

            services.AddHangfireServer();

            return services;
        }
    }
}
