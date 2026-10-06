using RWMS.Services;
using RWMS.Services.Implementations;
using RWMS.Services.Interfaces;

namespace RWMS.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddTransient<IEmailService, EmailService>();

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ISupplyService, SupplyService>();
        services.AddScoped<IFinanceService, FinanceService>();

        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddScoped<IInviteService, InviteService>();
        services.AddScoped<IOrderGuideService, OrderGuideService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAccountRequestService, AccountRequestService>();

        return services;
    }
}
