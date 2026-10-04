using FastEndpoints;
using FluentValidation;
using Pedidos.Application.Comum;

namespace Pedidos.Api.Comum;

/// <summary>Base dos requests de listagem: <c>?pagina=&amp;tamanhoPagina=</c>.</summary>
public abstract class PaginacaoRequest
{
    [QueryParam]
    public int Pagina { get; init; } = Paginacao.PaginaPadrao;

    [QueryParam]
    public int TamanhoPagina { get; init; } = Paginacao.TamanhoPaginaPadrao;

    public Paginacao ParaPaginacao() => new(Pagina, TamanhoPagina);
}

public static class PaginacaoValidacao
{
    /// <summary>Regras compartilhadas: <c>pagina</c> &gt;= 1 e <c>tamanhoPagina</c> entre 1 e 100.</summary>
    public static void AplicarRegrasDePaginacao<T>(this AbstractValidator<T> validator)
        where T : PaginacaoRequest
    {
        ArgumentNullException.ThrowIfNull(validator);

        validator.RuleFor(r => r.Pagina)
            .GreaterThanOrEqualTo(1)
            .WithMessage("pagina deve ser maior ou igual a 1.");

        validator.RuleFor(r => r.TamanhoPagina)
            .InclusiveBetween(1, Paginacao.TamanhoPaginaMaximo)
            .WithMessage($"tamanhoPagina deve estar entre 1 e {Paginacao.TamanhoPaginaMaximo}.");
    }
}
