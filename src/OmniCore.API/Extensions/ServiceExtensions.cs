using OmniCore.Application.Interfaces;
using OmniCore.Infrastructure.BackgroundJobs;
using OmniCore.Infrastructure.Services;

namespace OmniCore.API.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<OrderCleanupJob>();

            return services;
        }
    }
}
