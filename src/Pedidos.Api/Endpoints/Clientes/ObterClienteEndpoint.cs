using FastEndpoints;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Clientes.ObterCliente;

namespace Pedidos.Api.Endpoints.Clientes;

internal sealed class ObterClienteEndpoint(IUseCase<ObterClienteEntrada, ObterClienteSaida> casoDeUso)
    : Endpoint<ClientePorIdRequest, ClienteResponse>
{
    public override void Configure()
    {
        Get("clientes/{id}");
        Group<ApiV1>();
        Summary(s =>
        {
            s.Summary = "Obter cliente";
            s.Responses[StatusCodes.Status200OK] = "Cliente encontrado.";
            s.Responses[StatusCodes.Status404NotFound] = "Cliente inexistente.";
        });
    }

    public override async Task HandleAsync(ClientePorIdRequest req, CancellationToken ct)
    {
        var saida = await casoDeUso.ExecutarAsync(new ObterClienteEntrada(req.Id), ct);
        await Send.OkAsync(new ClienteResponse(saida.Id, saida.Nome, saida.Email, saida.CriadoEm), ct);
    }
}
