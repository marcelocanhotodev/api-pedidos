using FastEndpoints.Security;
using Pedidos.Infrastructure;
using Pedidos.Infrastructure.Seguranca;

namespace Pedidos.Api.Configuracao;

internal static class AutenticacaoConfig
{
    /// <summary>
    /// Valida a configuração de segurança (falha rápida) e liga JWT Bearer como exigência padrão:
    /// todo endpoint sem <c>AllowAnonymous()</c> exige token válido.
    /// </summary>
    public static IServiceCollection AddAutenticacaoJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var opcoes = OpcoesDeSeguranca.Carregar(configuration);

        services.AddSeguranca(opcoes);
        services
            .AddAuthenticationJwtBearer(
                s => s.SigningKey = opcoes.Chave,
                b =>
                {
                    var parametros = b.TokenValidationParameters;
                    parametros.ValidateIssuer = true;
                    parametros.ValidIssuer = opcoes.Emissor;
                    parametros.ValidateAudience = true;
                    parametros.ValidAudience = opcoes.Audiencia;
                    parametros.ValidateLifetime = true;
                    parametros.RequireExpirationTime = true;
                    // Sem tolerância: token expirado é recusado imediatamente.
                    parametros.ClockSkew = TimeSpan.Zero;
                })
            .AddAuthorization();

        return services;
    }
}
