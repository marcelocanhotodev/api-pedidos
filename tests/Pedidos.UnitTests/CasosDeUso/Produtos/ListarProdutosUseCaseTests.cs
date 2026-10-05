using Moq;
using Pedidos.Application.CasosDeUso.Produtos.ListarProdutos;
using Pedidos.Application.Comum;
using Pedidos.Domain.Entidades;
using Pedidos.Domain.Repositorios;

namespace Pedidos.UnitTests.CasosDeUso.Produtos;

/// <summary>Requisito: Consultar e Atualizar Produtos (listagem paginada e busca).</summary>
public class ListarProdutosUseCaseTests
{
    private static readonly DateTimeOffset CriadoEm = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly Mock<IProdutoRepository> _produtos = new();

    [Fact]
    public async Task ExecutarAsync_Pagina2Tamanho2_ConsultaComDeslocamentoEMontaEnvelope()
    {
        var produto = Produto.Restaurar(1, "CNT-1", "Caneta", 4.90m, 3, CriadoEm);
        _produtos.Setup(p => p.ListarAsync("cnt", 2, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginaDeProdutos([produto], Total: 5));

        var saida = await new ListarProdutosUseCase(_produtos.Object)
            .ExecutarAsync(new ListarProdutosEntrada("  cnt  ", new Paginacao(2, 2)), CancellationToken.None);

        Assert.Equal([new ProdutoResumo(1, "CNT-1", "Caneta", 4.90m, 3, CriadoEm)], saida.Pagina.Itens);
        Assert.Equal(5, saida.Pagina.TotalItens);
        Assert.Equal(3, saida.Pagina.TotalPaginas);
    }

    [Fact]
    public async Task ExecutarAsync_BuscaEmBranco_ListaTodos()
    {
        _produtos.Setup(p => p.ListarAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginaDeProdutos([], 0));

        await new ListarProdutosUseCase(_produtos.Object).ExecutarAsync(new ListarProdutosEntrada("   ", new Paginacao()), CancellationToken.None);

        _produtos.Verify(p => p.ListarAsync(null, 0, 20, It.IsAny<CancellationToken>()), Times.Once);
    }
}
