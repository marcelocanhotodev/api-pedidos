using Moq;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Produtos.AtualizarProduto;
using Pedidos.Domain.Entidades;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.UnitTests.CasosDeUso.Produtos;

/// <summary>Requisito: Consultar e Atualizar Produtos (atualização).</summary>
public class AtualizarProdutoUseCaseTests
{
    private const int Id = 7;
    private static readonly DateTimeOffset CriadoEm = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly Mock<IProdutoRepository> _produtos = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public AtualizarProdutoUseCaseTests() =>
        _produtos.Setup(p => p.ObterAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Produto.Restaurar(Id, "CNT-1", "Caneta", 4.90m, 15, CriadoEm));

    private AtualizarProdutoUseCase CriarCasoDeUso() => new(_produtos.Object, _unitOfWork.Object);

    [Fact]
    public async Task ExecutarAsync_NomeEPreco_GravaSoNomeEPrecoMantendoSkuEEstoque()
    {
        var saida = await CriarCasoDeUso().ExecutarAsync(new AtualizarProdutoEntrada(Id, " Caneta azul ", 5.5m), CancellationToken.None);

        Assert.Equal(new AtualizarProdutoSaida(Id, "CNT-1", "Caneta azul", 5.50m, 15, CriadoEm), saida);
        _produtos.Verify(p => p.AtualizarNomeEPrecoAsync(It.Is<Produto>(x => x.Nome == "Caneta azul" && x.Preco == 5.50m), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ProdutoInexistente_LancaNaoEncontradoEDesfaz()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(
            () => CriarCasoDeUso().ExecutarAsync(new AtualizarProdutoEntrada(999, "Caneta", 5m), CancellationToken.None));

        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_PrecoInvalido_LancaRegraDeNegocioSemGravar()
    {
        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => CriarCasoDeUso().ExecutarAsync(new AtualizarProdutoEntrada(Id, "Caneta", -1m), CancellationToken.None));

        _produtos.Verify(p => p.AtualizarNomeEPrecoAsync(It.IsAny<Produto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
