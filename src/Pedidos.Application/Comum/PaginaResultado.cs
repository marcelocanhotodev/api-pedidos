namespace Pedidos.Application.Comum;

/// <summary>Envelope padrão de toda listagem paginada.</summary>
public sealed record PaginaResultado<T>(
    IReadOnlyList<T> Itens,
    int Pagina,
    int TamanhoPagina,
    long TotalItens,
    int TotalPaginas);

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
