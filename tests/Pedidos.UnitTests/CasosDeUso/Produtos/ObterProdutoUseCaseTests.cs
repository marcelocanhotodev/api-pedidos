using Moq;
using Pedidos.Application.CasosDeUso.Produtos.ObterProduto;
using Pedidos.Domain.Entidades;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.UnitTests.CasosDeUso.Produtos;

/// <summary>Requisito: Consultar e Atualizar Produtos (obter por id).</summary>
public class ObterProdutoUseCaseTests
{
    private readonly Mock<IProdutoRepository> _produtos = new();

    [Fact]
    public async Task ExecutarAsync_ProdutoExiste_RetornaComEstoque()
    {
        var criadoEm = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _produtos.Setup(p => p.ObterAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Produto.Restaurar(7, "CNT-1", "Caneta", 4.90m, 15, criadoEm));

        var saida = await new ObterProdutoUseCase(_produtos.Object).ExecutarAsync(new ObterProdutoEntrada(7), CancellationToken.None);

        Assert.Equal(new ObterProdutoSaida(7, "CNT-1", "Caneta", 4.90m, 15, criadoEm), saida);
    }

    [Fact]
    public async Task ExecutarAsync_ProdutoInexistente_LancaNaoEncontrado()
    {
        var excecao = await Assert.ThrowsAsync<NaoEncontradoException>(
            () => new ObterProdutoUseCase(_produtos.Object).ExecutarAsync(new ObterProdutoEntrada(999), CancellationToken.None));

        Assert.Equal("Produto", excecao.Recurso);
    }
}
