using System.Reflection;
using Pedidos.Application.Abstracoes;

namespace Pedidos.Api.Configuracao;

internal sealed class InformacoesDaAplicacao(IHostEnvironment ambiente) : IInformacoesDaAplicacao
{
    public string Nome => "API de Pedidos";

    public string Versao { get; } =
        typeof(InformacoesDaAplicacao).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "desconhecida";

    public string Ambiente => ambiente.EnvironmentName;
}
