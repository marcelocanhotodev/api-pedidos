using System.Reflection;
using Pedidos.Application.Abstracoes;

namespace Pedidos.Api.Configuracao;

internal sealed class InformacoesDaAplicacao(IHostEnvironment ambiente, IConfiguration configuracao) : IInformacoesDaAplicacao
{
    /// <summary>Definida pelo Render a cada deploy com o commit implantado.</summary>
    public const string VariavelCommitRender = "RENDER_GIT_COMMIT";

    /// <summary>Alternativa para outros ambientes (ex.: build local ou outro provedor).</summary>
    public const string VariavelCommit = "VERSAO_COMMIT";

    private const int TamanhoCommitCurto = 7;

    private static readonly string VersaoDoAssembly =
        typeof(InformacoesDaAplicacao).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "desconhecida";

    public string Nome => "API de Pedidos";

    /// <summary><c>&lt;versão&gt;+&lt;7 caracteres do commit&gt;</c> quando o commit implantado é conhecido.</summary>
    public string Versao { get; } = MontarVersao(VersaoDoAssembly, configuracao[VariavelCommitRender] ?? configuracao[VariavelCommit]);

    public string Ambiente => ambiente.EnvironmentName;

    internal static string MontarVersao(string versao, string? commit)
    {
        var commitLimpo = commit?.Trim();
        return string.IsNullOrEmpty(commitLimpo)
            ? versao
            : $"{versao}+{commitLimpo[..Math.Min(TamanhoCommitCurto, commitLimpo.Length)]}";
    }
}
