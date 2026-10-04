using Microsoft.Extensions.Configuration;

namespace Pedidos.Infrastructure.Seguranca;

/// <summary>Configuração de JWT e do usuário autorizado, lida das variáveis de ambiente.</summary>
public sealed class OpcoesDeSeguranca
{
    public const int TamanhoMinimoDaChave = 32;

    public required string Chave { get; init; }

    public required string Emissor { get; init; }

    public required string Audiencia { get; init; }

    public required int ExpiraMinutos { get; init; }

    public required string Usuario { get; init; }

    public required string Senha { get; init; }

    /// <summary>
    /// Lê e valida <c>JWT_CHAVE</c>, <c>JWT_EMISSOR</c>, <c>JWT_AUDIENCIA</c>, <c>JWT_EXPIRA_MINUTOS</c>,
    /// <c>AUTH_USUARIO</c> e <c>AUTH_SENHA</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Alguma variável está ausente ou inválida.</exception>
    public static OpcoesDeSeguranca Carregar(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var erros = new List<string>();

        var chave = configuration["JWT_CHAVE"];
        if (string.IsNullOrWhiteSpace(chave))
        {
            erros.Add("JWT_CHAVE não foi definida");
        }
        else if (chave.Length < TamanhoMinimoDaChave)
        {
            erros.Add($"JWT_CHAVE deve ter no mínimo {TamanhoMinimoDaChave} caracteres (tem {chave.Length})");
        }

        var emissor = Obrigatoria(configuration, "JWT_EMISSOR", erros);
        var audiencia = Obrigatoria(configuration, "JWT_AUDIENCIA", erros);
        var usuario = Obrigatoria(configuration, "AUTH_USUARIO", erros);
        var senha = Obrigatoria(configuration, "AUTH_SENHA", erros);

        if (!int.TryParse(configuration["JWT_EXPIRA_MINUTOS"], out var expiraMinutos) || expiraMinutos <= 0)
        {
            erros.Add("JWT_EXPIRA_MINUTOS deve ser um número inteiro maior que zero");
        }

        if (erros.Count > 0)
        {
            throw new InvalidOperationException("Configuração de segurança inválida: " + string.Join("; ", erros) + ".");
        }

        return new OpcoesDeSeguranca
        {
            Chave = chave!,
            Emissor = emissor,
            Audiencia = audiencia,
            ExpiraMinutos = expiraMinutos,
            Usuario = usuario,
            Senha = senha,
        };
    }

    private static string Obrigatoria(IConfiguration configuration, string chave, List<string> erros)
    {
        var valor = configuration[chave];
        if (string.IsNullOrWhiteSpace(valor))
        {
            erros.Add($"{chave} não foi definida");
            return string.Empty;
        }

        return valor;
    }
}
