using FastEndpoints;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Clientes.AtualizarCliente;

namespace Pedidos.Api.Endpoints.Clientes;

public sealed class AtualizarClienteRequest : DadosDoClienteRequest
{
    public int Id { get; init; }
}

public sealed class AtualizarClienteValidator : Validator<AtualizarClienteRequest>
{
    public AtualizarClienteValidator() => this.AplicarRegrasDoCliente();
}

internal sealed class AtualizarClienteEndpoint(IUseCase<AtualizarClienteEntrada, AtualizarClienteSaida> casoDeUso)
    : Endpoint<AtualizarClienteRequest, ClienteResponse>
{
    public override void Configure()
    {
        Put("clientes/{id}");
        Group<ApiV1>();
        Summary(s =>
        {
            s.Summary = "Atualizar cliente";
            s.Description = "Substitui nome e e-mail do cliente.";
            s.Responses[StatusCodes.Status200OK] = "Cliente atualizado.";
            s.Responses[StatusCodes.Status400BadRequest] = "Nome ou e-mail inválido.";
            s.Responses[StatusCodes.Status404NotFound] = "Cliente inexistente.";
            s.Responses[StatusCodes.Status409Conflict] = "E-mail pertence a outro cliente.";
        });
    }

    public override async Task HandleAsync(AtualizarClienteRequest req, CancellationToken ct)
    {
        var saida = await casoDeUso.ExecutarAsync(new AtualizarClienteEntrada(req.Id, req.Nome, req.Email), ct);
        await Send.OkAsync(new ClienteResponse(saida.Id, saida.Nome, saida.Email, saida.CriadoEm), ct);
    }
}
