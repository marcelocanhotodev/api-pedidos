using FastEndpoints;

namespace Pedidos.Api.Endpoints;

/// <summary>
/// Grupo dos endpoints de negócio: prefixo <c>/api/v1</c>. Endpoints fora do grupo (ex.: <c>GET /info</c>)
/// mantêm suas rotas sem prefixo.
/// </summary>
public sealed class ApiV1 : Group
{
    public const string Prefixo = "api/v1";

    public ApiV1()
    {
        Configure(Prefixo, _ => { });
    }
}
