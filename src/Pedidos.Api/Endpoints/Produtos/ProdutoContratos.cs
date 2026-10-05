using FastEndpoints;
using FluentValidation;
using Pedidos.Domain.Entidades;

namespace Pedidos.Api.Endpoints.Produtos;

/// <summary>Representação de um produto nas respostas da API.</summary>
public sealed record ProdutoResponse(int Id, string Sku, string Nome, decimal Preco, int Estoque, DateTimeOffset CriadoEm);

/// <summary>Request dos endpoints que recebem apenas o id do produto pela rota.</summary>
public sealed class ProdutoPorIdRequest
{
    public int Id { get; init; }
}

public static class ProdutoValidacao
{
    public static readonly string MensagemSku =
        $"sku deve ter de 1 a {Produto.SkuTamanhoMaximo} caracteres, apenas letras, números, '-', '_' e '.'.";

    public static readonly string MensagemNome = $"nome deve ter entre 1 e {Produto.NomeTamanhoMaximo} caracteres.";

    public static readonly string MensagemPreco =
        $"preco é obrigatório, deve estar entre 0 e {Produto.PrecoMaximo} e ter no máximo 2 casas decimais.";

    public static readonly string MensagemEstoque = $"estoque deve estar entre 0 e {Produto.EstoqueMaximo}.";

    public static readonly string MensagemDelta =
        $"delta deve ser diferente de zero e estar entre -{Produto.DeltaMaximo} e {Produto.DeltaMaximo}.";

    /// <summary>Mesmas regras da entidade <see cref="Produto"/> para nome e preço, respondidas como 400 por campo.</summary>
    public static void AplicarRegrasDeNomeEPreco<T>(
        this AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, string>> nome,
        System.Linq.Expressions.Expression<Func<T, decimal?>> preco)
    {
        ArgumentNullException.ThrowIfNull(validator);

        validator.RuleFor(nome).Must(Produto.NomeEhValido).WithMessage(MensagemNome);
        validator.RuleFor(preco)
            .Must(p => p.HasValue && Produto.PrecoEhValido(p.Value))
            .WithMessage(MensagemPreco);
    }
}
