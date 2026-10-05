using FastEndpoints;
using FluentValidation;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Produtos.AjustarEstoqueProduto;
using Pedidos.Domain.Entidades;

namespace Pedidos.Api.Endpoints.Produtos;

public sealed class AjustarEstoqueProdutoRequest
{
    public int Id { get; init; }

    /// <summary>Quantidade a somar (positiva) ou subtrair (negativa).</summary>
    public int Delta { get; init; }
}

public sealed class AjustarEstoqueProdutoValidator : Validator<AjustarEstoqueProdutoRequest>
{
    public AjustarEstoqueProdutoValidator()
    {
        RuleFor(r => r.Delta).Must(Produto.DeltaEhValido).WithMessage(ProdutoValidacao.MensagemDelta);
    }
}

internal sealed class AjustarEstoqueProdutoEndpoint(IUseCase<AjustarEstoqueProdutoEntrada, AjustarEstoqueProdutoSaida> casoDeUso)
    : Endpoint<AjustarEstoqueProdutoRequest, ProdutoResponse>
{
    public override void Configure()
    {
        Patch("produtos/{id}/estoque");
        Group<ApiV1>();
        Summary(s =>
        {
            s.Summary = "Ajustar estoque";
            s.Description = "Soma o delta ao estoque de forma atômica (ajustes simultâneos nunca se perdem) e devolve o produto atualizado.";
            s.ExampleRequest = new AjustarEstoqueProdutoRequest { Delta = 10 };
            s.Responses[StatusCodes.Status200OK] = "Estoque ajustado; o corpo traz o estoque resultante.";
            s.Responses[StatusCodes.Status400BadRequest] = "Delta zero ou fora do limite.";
            s.Responses[StatusCodes.Status404NotFound] = "Produto inexistente.";
            s.Responses[StatusCodes.Status422UnprocessableEntity] = "O estoque ficaria negativo ou acima do máximo.";
        });
    }

    public override async Task HandleAsync(AjustarEstoqueProdutoRequest req, CancellationToken ct)
    {
        var p = await casoDeUso.ExecutarAsync(new AjustarEstoqueProdutoEntrada(req.Id, req.Delta), ct);
        await Send.OkAsync(new ProdutoResponse(p.Id, p.Sku, p.Nome, p.Preco, p.Estoque, p.CriadoEm), ct);
    }
}
