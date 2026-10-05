using FastEndpoints;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Clientes.ExcluirCliente;

namespace Pedidos.Api.Endpoints.Clientes;

internal sealed class ExcluirClienteEndpoint(IUseCase<ExcluirClienteEntrada, Vazio> casoDeUso)
    : Endpoint<ClientePorIdRequest>
{
    public override void Configure()
    {
        Delete("clientes/{id}");
        Group<ApiV1>();
        Summary(s =>
        {
            s.Summary = "Excluir cliente";
            s.Responses[StatusCodes.Status204NoContent] = "Cliente excluído.";
            s.Responses[StatusCodes.Status404NotFound] = "Cliente inexistente.";
        });
    }

    public override async Task HandleAsync(ClientePorIdRequest req, CancellationToken ct)
    {
        await casoDeUso.ExecutarAsync(new ExcluirClienteEntrada(req.Id), ct);
        await Send.NoContentAsync(ct);
    }
}
