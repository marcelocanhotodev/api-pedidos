using FastEndpoints;
using FluentValidation;
using Pedidos.Api.Comum;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Produtos.ListarProdutos;
using Pedidos.Application.Comum;
using Pedidos.Domain.Entidades;

namespace Pedidos.Api.Endpoints.Produtos;

public sealed class ListarProdutosRequest : PaginacaoRequest
{
    [QueryParam]
    public string? Busca { get; init; }
}

public sealed class ListarProdutosValidator : Validator<ListarProdutosRequest>
{
    public ListarProdutosValidator()
    {
        this.AplicarRegrasDePaginacao();
        RuleFor(r => r.Busca)
            .MaximumLength(Produto.NomeTamanhoMaximo)
            .WithMessage($"busca deve ter no máximo {Produto.NomeTamanhoMaximo} caracteres.");
    }
}

internal sealed class ListarProdutosEndpoint(IUseCase<ListarProdutosEntrada, ListarProdutosSaida> casoDeUso)
    : Endpoint<ListarProdutosRequest, PaginaResultado<ProdutoResponse>>
{
    public override void Configure()
    {
        Get("produtos");
        Group<ApiV1>();
        Summary(s =>
        {
            s.Summary = "Listar produtos";
            s.Description = "Lista paginada, ordenada por nome; o parâmetro busca filtra nome ou SKU sem diferenciar maiúsculas.";
            s.Responses[StatusCodes.Status200OK] = "Página de produtos.";
            s.Responses[StatusCodes.Status400BadRequest] = "Parâmetros de paginação ou busca inválidos.";
        });
    }

    public override async Task HandleAsync(ListarProdutosRequest req, CancellationToken ct)
    {
        var saida = await casoDeUso.ExecutarAsync(new ListarProdutosEntrada(req.Busca, req.ParaPaginacao()), ct);
        await Send.OkAsync(saida.Pagina.Mapear(p => new ProdutoResponse(p.Id, p.Sku, p.Nome, p.Preco, p.Estoque, p.CriadoEm)), ct);
    }
}
