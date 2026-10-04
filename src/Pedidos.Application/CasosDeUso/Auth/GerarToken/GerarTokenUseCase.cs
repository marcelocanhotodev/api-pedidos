using Pedidos.Application.Abstracoes;
using Pedidos.Application.Excecoes;

namespace Pedidos.Application.CasosDeUso.Auth.GerarToken;

public sealed class GerarTokenUseCase(IValidadorDeCredenciais validador, IEmissorDeToken emissor)
    : IUseCase<GerarTokenEntrada, GerarTokenSaida>
{
    public Task<GerarTokenSaida> ExecutarAsync(GerarTokenEntrada entrada, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        if (!validador.Validar(entrada.Usuario, entrada.Senha))
        {
            throw new CredenciaisInvalidasException();
        }

        var token = emissor.Emitir(entrada.Usuario);
        return Task.FromResult(new GerarTokenSaida(token.AccessToken, token.ExpiraEmSegundos));
    }
}
