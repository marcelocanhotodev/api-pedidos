using Microsoft.Extensions.DependencyInjection;
using Pedidos.Application;
using Pedidos.Application.Abstracoes;

namespace Pedidos.UnitTests.Application;

/// <summary>Requisito: Registro dos Casos de Uso.</summary>
public class RegistroDosCasosDeUsoTests
{
    [Fact]
    public void AddCasosDeUso_NovaClasseQueImplementaIUseCase_FicaDisponivelParaInjecao()
    {
        var services = new ServiceCollection();

        services.AddCasosDeUso(typeof(SomarUseCase).Assembly);
        using var provider = services.BuildServiceProvider();
        using var escopo = provider.CreateScope();

        var casoDeUso = escopo.ServiceProvider.GetService<IUseCase<SomarEntrada, SomarSaida>>();

        Assert.IsType<SomarUseCase>(casoDeUso);
    }

    [Fact]
    public void AddCasosDeUso_CasoDeUso_RegistradoComoScoped()
    {
        var services = new ServiceCollection();

        services.AddCasosDeUso(typeof(SomarUseCase).Assembly);

        var descritor = Assert.Single(services, d => d.ServiceType == typeof(IUseCase<SomarEntrada, SomarSaida>));
        Assert.Equal(ServiceLifetime.Scoped, descritor.Lifetime);
    }

    [Fact]
    public void AddApplication_AssemblyDaApplication_NaoLancaExcecao()
    {
        var services = new ServiceCollection();

        var resultado = services.AddApplication();

        Assert.Same(services, resultado);
    }

    public sealed record SomarEntrada(int A, int B);

    public sealed record SomarSaida(int Total);

    public sealed class SomarUseCase : IUseCase<SomarEntrada, SomarSaida>
    {
        public Task<SomarSaida> ExecutarAsync(SomarEntrada entrada, CancellationToken ct) =>
            Task.FromResult(new SomarSaida(entrada.A + entrada.B));
    }
}
