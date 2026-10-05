using FastEndpoints;
using FluentValidation;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Produtos.CriarProduto;
using Pedidos.Domain.Entidades;

namespace Pedidos.Api.Endpoints.Produtos;

public sealed class CriarProdutoRequest
{
    public string Sku { get; init; } = string.Empty;

    public string Nome { get; init; } = string.Empty;

    /// <summary>Obrigatório: sem ele o produto teria preço 0 sem aviso.</summary>
    public decimal? Preco { get; init; }

    public int Estoque { get; init; }
}

public sealed class CriarProdutoValidator : Validator<CriarProdutoRequest>
{
    public CriarProdutoValidator()
    {
        RuleFor(r => r.Sku).Must(Produto.SkuEhValido).WithMessage(ProdutoValidacao.MensagemSku);
        this.AplicarRegrasDeNomeEPreco(r => r.Nome, r => r.Preco);
        RuleFor(r => r.Estoque).Must(Produto.EstoqueEhValido).WithMessage(ProdutoValidacao.MensagemEstoque);
    }
}

internal sealed class CriarProdutoEndpoint(IUseCase<CriarProdutoEntrada, CriarProdutoSaida> casoDeUso)
    : Endpoint<CriarProdutoRequest, ProdutoResponse>
{
    public override void Configure()
    {
        Post("produtos");
        Group<ApiV1>();
        Summary(s =>
        {
            s.Summary = "Criar produto";
            s.Description = "Cadastra um produto. O SKU é gravado sem espaços nas pontas e em maiúsculas; o preço, sempre com 2 casas.";
            s.ExampleRequest = new CriarProdutoRequest { Sku = "CNT-AZUL-01", Nome = "Caneta azul", Preco = 4.90m, Estoque = 100 };
            s.Responses[StatusCodes.Status201Created] = "Produto criado; o cabeçalho Location aponta para o recurso.";
            s.Responses[StatusCodes.Status400BadRequest] = "SKU, nome, preço ou estoque inválido.";
            s.Responses[StatusCodes.Status409Conflict] = "SKU já cadastrado.";
        });
    }

    public override async Task HandleAsync(CriarProdutoRequest req, CancellationToken ct)
    {
        var p = await casoDeUso.ExecutarAsync(new CriarProdutoEntrada(req.Sku, req.Nome, req.Preco!.Value, req.Estoque), ct);
        await Send.CreatedAtAsync<ObterProdutoEndpoint>(
            new { id = p.Id },
            new ProdutoResponse(p.Id, p.Sku, p.Nome, p.Preco, p.Estoque, p.CriadoEm),
            cancellation: ct);
    }
}
