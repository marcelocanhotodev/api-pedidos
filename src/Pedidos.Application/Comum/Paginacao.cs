namespace Pedidos.Application.Comum;

/// <summary>Parâmetros de paginação recebidos por toda listagem.</summary>
public sealed record Paginacao(int Pagina = Paginacao.PaginaPadrao, int TamanhoPagina = Paginacao.TamanhoPaginaPadrao)
{
    public const int PaginaPadrao = 1;
    public const int TamanhoPaginaPadrao = 20;
    public const int TamanhoPaginaMaximo = 100;

    /// <summary>Quantidade de itens a pular (para <c>OFFSET</c>).</summary>
    public int Deslocamento => (Pagina - 1) * TamanhoPagina;
}
