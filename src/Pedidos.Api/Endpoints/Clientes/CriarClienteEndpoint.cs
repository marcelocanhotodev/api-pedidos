using FastEndpoints;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Clientes.CriarCliente;

namespace Pedidos.Api.Endpoints.Clientes;

public sealed class CriarClienteRequest : DadosDoClienteRequest;

public sealed class CriarClienteValidator : Validator<CriarClienteRequest>
{
    public CriarClienteValidator() => this.AplicarRegrasDoCliente();
}

internal sealed class CriarClienteEndpoint(IUseCase<CriarClienteEntrada, CriarClienteSaida> casoDeUso)
    : Endpoint<CriarClienteRequest, ClienteResponse>
{
    public override void Configure()
    {
        Post("clientes");
        Group<ApiV1>();
        Summary(s =>
        {
            s.Summary = "Criar cliente";
            s.Description = "Cadastra um cliente; nome e e-mail são gravados sem espaços nas pontas.";
            s.ExampleRequest = new CriarClienteRequest { Nome = "Ana Souza", Email = "ana@exemplo.com" };
            s.Responses[StatusCodes.Status201Created] = "Cliente criado; o cabeçalho Location aponta para o recurso.";
            s.Responses[StatusCodes.Status400BadRequest] = "Nome ou e-mail inválido.";
            s.Responses[StatusCodes.Status409Conflict] = "E-mail já cadastrado.";
        });
    }

    public override async Task HandleAsync(CriarClienteRequest req, CancellationToken ct)
    {
        var saida = await casoDeUso.ExecutarAsync(new CriarClienteEntrada(req.Nome, req.Email), ct);
        await Send.CreatedAtAsync<ObterClienteEndpoint>(
            new { id = saida.Id },
            new ClienteResponse(saida.Id, saida.Nome, saida.Email, saida.CriadoEm),
            cancellation: ct);
    }
}
