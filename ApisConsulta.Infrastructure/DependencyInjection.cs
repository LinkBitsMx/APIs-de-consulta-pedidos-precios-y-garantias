using ApisConsulta.Application.Interfaces;
using ApisConsulta.Infrastructure.Persistence;
using ApisConsulta.Infrastructure.Repositories;
using ApisConsulta.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApisConsulta.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IEnvioRepository, EnvioRepository>();
        services.AddScoped<IPrecioRepository, PrecioRepository>();
        services.AddScoped<IGarantiaRepository, GarantiaRepository>();
        services.AddScoped<IPreOrdenRepository, PreOrdenRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
