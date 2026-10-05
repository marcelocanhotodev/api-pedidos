namespace Pedidos.Application.Comum;

/// <summary>Envelope padrão de toda listagem paginada.</summary>
public sealed record PaginaResultado<T>(
    IReadOnlyList<T> Itens,
    int Pagina,
    int TamanhoPagina,
    long TotalItens,
    int TotalPaginas)
{
    /// <summary>Mesma página com os itens convertidos.</summary>
    public PaginaResultado<TDestino> Mapear<TDestino>(Func<T, TDestino> conversor) =>
        new(Itens.Select(conversor).ToList(), Pagina, TamanhoPagina, TotalItens, TotalPaginas);
}

public static class PaginaResultado
{
    public static PaginaResultado<T> Criar<T>(IReadOnlyList<T> itens, Paginacao paginacao, long totalItens)
    {
        ArgumentNullException.ThrowIfNull(itens);
        ArgumentNullException.ThrowIfNull(paginacao);

        var totalPaginas = (int)((totalItens + paginacao.TamanhoPagina - 1) / paginacao.TamanhoPagina);
        return new PaginaResultado<T>(itens, paginacao.Pagina, paginacao.TamanhoPagina, totalItens, totalPaginas);
    }
}
