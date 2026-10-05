using FastEndpoints;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Produtos.AtualizarProduto;

namespace Pedidos.Api.Endpoints.Produtos;

/// <summary>Só nome e preço: SKU é imutável e estoque muda apenas por PATCH .../estoque.</summary>
public sealed class AtualizarProdutoRequest
{
    public int Id { get; init; }

    public string Nome { get; init; } = string.Empty;

    public decimal? Preco { get; init; }
}

public sealed class AtualizarProdutoValidator : Validator<AtualizarProdutoRequest>
{
    public AtualizarProdutoValidator() => this.AplicarRegrasDeNomeEPreco(r => r.Nome, r => r.Preco);
}

internal sealed class AtualizarProdutoEndpoint(IUseCase<AtualizarProdutoEntrada, AtualizarProdutoSaida> casoDeUso)
    : Endpoint<AtualizarProdutoRequest, ProdutoResponse>
{
    public override void Configure()
    {
        Put("produtos/{id}");
        Group<ApiV1>();
        Summary(s =>
        {
            s.Summary = "Atualizar produto";
            s.Description = "Substitui nome e preço. SKU e estoque não mudam por aqui (use PATCH /produtos/{id}/estoque).";
            s.Responses[StatusCodes.Status200OK] = "Produto atualizado.";
            s.Responses[StatusCodes.Status400BadRequest] = "Nome ou preço inválido.";
            s.Responses[StatusCodes.Status404NotFound] = "Produto inexistente.";
        });
    }

    public override async Task HandleAsync(AtualizarProdutoRequest req, CancellationToken ct)
    {
        var p = await casoDeUso.ExecutarAsync(new AtualizarProdutoEntrada(req.Id, req.Nome, req.Preco!.Value), ct);
        await Send.OkAsync(new ProdutoResponse(p.Id, p.Sku, p.Nome, p.Preco, p.Estoque, p.CriadoEm), ct);
    }
}
