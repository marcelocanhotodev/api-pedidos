using Pedidos.Application.Comum;

namespace Pedidos.UnitTests.Application;

/// <summary>Requisito: Paginação.</summary>
public class PaginaResultadoTests
{
    [Fact]
    public void Paginacao_SemParametros_UsaPagina1ETamanho20()
    {
        var paginacao = new Paginacao();

        Assert.Equal(1, paginacao.Pagina);
        Assert.Equal(20, paginacao.TamanhoPagina);
        Assert.Equal(0, paginacao.Deslocamento);
    }

    [Fact]
    public void Paginacao_Pagina3Tamanho20_Desloca40Itens()
    {
        Assert.Equal(40, new Paginacao(3, 20).Deslocamento);
    }

    [Theory]
    [InlineData(45, 20, 3)]
    [InlineData(40, 20, 2)]
    [InlineData(1, 20, 1)]
    [InlineData(0, 20, 0)]
    public void Criar_TotalDeItens_CalculaTotalDePaginas(long totalItens, int tamanhoPagina, int totalPaginasEsperado)
    {
        var resultado = PaginaResultado.Criar<string>([], new Paginacao(1, tamanhoPagina), totalItens);

        Assert.Equal(totalItens, resultado.TotalItens);
        Assert.Equal(totalPaginasEsperado, resultado.TotalPaginas);
    }

    [Fact]
    public void Criar_ItensEParametros_PreservaPaginaETamanho()
    {
        var resultado = PaginaResultado.Criar(["a", "b"], new Paginacao(2, 2), 5);

        Assert.Equal(["a", "b"], resultado.Itens);
        Assert.Equal(2, resultado.Pagina);
        Assert.Equal(2, resultado.TamanhoPagina);
        Assert.Equal(3, resultado.TotalPaginas);
    }
}
