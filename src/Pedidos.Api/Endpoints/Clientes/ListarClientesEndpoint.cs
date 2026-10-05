using FastEndpoints;
using FluentValidation;
using Pedidos.Api.Comum;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Clientes.ListarClientes;
using Pedidos.Application.Comum;
using Pedidos.Domain.Entidades;

namespace Pedidos.Api.Endpoints.Clientes;

public sealed class ListarClientesRequest : PaginacaoRequest
{
    [QueryParam]
    public string? Busca { get; init; }
}

public sealed class ListarClientesValidator : Validator<ListarClientesRequest>
{
    public ListarClientesValidator()
    {
        this.AplicarRegrasDePaginacao();
        RuleFor(r => r.Busca)
            .MaximumLength(Cliente.EmailTamanhoMaximo)
            .WithMessage($"busca deve ter no máximo {Cliente.EmailTamanhoMaximo} caracteres.");
    }
}

internal sealed class ListarClientesEndpoint(IUseCase<ListarClientesEntrada, ListarClientesSaida> casoDeUso)
    : Endpoint<ListarClientesRequest, PaginaResultado<ClienteResponse>>
{
    public override void Configure()
    {
        Get("clientes");
        Group<ApiV1>();
        Summary(s =>
        {
            s.Summary = "Listar clientes";
            s.Description = "Lista paginada, ordenada por nome; o parâmetro busca filtra nome ou e-mail sem diferenciar maiúsculas.";
            s.Responses[StatusCodes.Status200OK] = "Página de clientes.";
            s.Responses[StatusCodes.Status400BadRequest] = "Parâmetros de paginação ou busca inválidos.";
        });
    }

    public override async Task HandleAsync(ListarClientesRequest req, CancellationToken ct)
    {
        var saida = await casoDeUso.ExecutarAsync(new ListarClientesEntrada(req.Busca, req.ParaPaginacao()), ct);
        await Send.OkAsync(saida.Pagina.Mapear(c => new ClienteResponse(c.Id, c.Nome, c.Email, c.CriadoEm)), ct);
    }
}
