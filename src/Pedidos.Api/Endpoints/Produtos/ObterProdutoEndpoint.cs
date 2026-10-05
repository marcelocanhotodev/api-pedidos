using FastEndpoints;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Produtos.ObterProduto;

namespace Pedidos.Api.Endpoints.Produtos;

internal sealed class ObterProdutoEndpoint(IUseCase<ObterProdutoEntrada, ObterProdutoSaida> casoDeUso)
    : Endpoint<ProdutoPorIdRequest, ProdutoResponse>
{
    public override void Configure()
    {
        Get("produtos/{id}");
        Group<ApiV1>();
        Summary(s =>
        {
            s.Summary = "Obter produto";
            s.Responses[StatusCodes.Status200OK] = "Produto encontrado, com o estoque atual.";
            s.Responses[StatusCodes.Status404NotFound] = "Produto inexistente.";
        });
    }

    public override async Task HandleAsync(ProdutoPorIdRequest req, CancellationToken ct)
    {
        var p = await casoDeUso.ExecutarAsync(new ObterProdutoEntrada(req.Id), ct);
        await Send.OkAsync(new ProdutoResponse(p.Id, p.Sku, p.Nome, p.Preco, p.Estoque, p.CriadoEm), ct);
    }
}
