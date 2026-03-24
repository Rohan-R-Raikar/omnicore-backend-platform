using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace OmniCore.API.Extensions
{
    public static class RateLimiterExtensions
    {
        public static IServiceCollection AddRateLimiterConfig(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                    // Global fallback limiter
                    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 100,
                                Window = TimeSpan.FromMinutes(1),
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 2
                            }));

                    // Auth endpoints (STRICT)
                    options.AddFixedWindowLimiter("authPolicy", opt =>
                    {
                        opt.PermitLimit = 5;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueLimit = 0;
                    });

                    // Order endpoints (MODERATE)
                    options.AddFixedWindowLimiter("orderPolicy", opt =>
                    {
                        opt.PermitLimit = 20;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueLimit = 2;
                    });

                    // General usage (RELAXED)
                    options.AddFixedWindowLimiter("relaxedPolicy", opt =>
                    {
                        opt.PermitLimit = 50;
                        opt.Window = TimeSpan.FromMinutes(1);
                    });

                    // When limit exceeded
                    options.OnRejected = async (context, token) =>
                    {
                        context.HttpContext.Response.StatusCode = 429;

                        await context.HttpContext.Response.WriteAsJsonAsync(new
                        {
                            status = 429,
                            message = "Too many requests. Please try again later."
                        }, token);
                    };
                });

            return services;
        }
    }
}
