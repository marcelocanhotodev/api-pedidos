using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pedidos.Application.Abstracoes;

namespace Pedidos.Application;

public static class DependencyInjection
{
    /// <summary>Registra todos os casos de uso da Application.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services.AddCasosDeUso(typeof(DependencyInjection).Assembly);

    /// <summary>
    /// Registra como <c>Scoped</c> toda classe concreta do assembly que implementa <see cref="IUseCase{TEntrada,TSaida}"/>,
    /// sob a interface fechada correspondente.
    /// </summary>
    public static IServiceCollection AddCasosDeUso(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var registros =
            from tipo in assembly.GetTypes()
            where tipo is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
            from interfaceDoTipo in tipo.GetInterfaces()
            where interfaceDoTipo.IsGenericType && interfaceDoTipo.GetGenericTypeDefinition() == typeof(IUseCase<,>)
            select (Servico: interfaceDoTipo, Implementacao: tipo);

        foreach (var (servico, implementacao) in registros)
        {
            services.AddScoped(servico, implementacao);
        }

        return services;
    }
}
