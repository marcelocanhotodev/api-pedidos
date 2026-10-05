using Moq;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Produtos.AjustarEstoqueProduto;
using Pedidos.Domain.Entidades;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.UnitTests.CasosDeUso.Produtos;

/// <summary>Requisito: Ajustar Estoque.</summary>
public class AjustarEstoqueProdutoUseCaseTests
{
    private static readonly DateTimeOffset CriadoEm = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly Mock<IProdutoRepository> _produtos = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AjustarEstoqueProdutoUseCase CriarCasoDeUso() => new(_produtos.Object, _unitOfWork.Object);

    [Fact]
    public async Task ExecutarAsync_DeltaValido_AplicaPeloRepositorioEDevolveEstoqueResultante()
    {
        _produtos.Setup(p => p.AjustarEstoqueAsync(7, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Produto.Restaurar(7, "CNT-1", "Caneta", 4.90m, 15, CriadoEm));

        var saida = await CriarCasoDeUso().ExecutarAsync(new AjustarEstoqueProdutoEntrada(7, 10), CancellationToken.None);

        Assert.Equal(15, saida.Estoque);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1_000_001)]
    public async Task ExecutarAsync_DeltaInvalido_LancaRegraDeNegocioSemAbrirTransacao(int delta)
    {
        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => CriarCasoDeUso().ExecutarAsync(new AjustarEstoqueProdutoEntrada(7, delta), CancellationToken.None));

        _unitOfWork.Verify(u => u.IniciarAsync(It.IsAny<CancellationToken>()), Times.Never);
        _produtos.Verify(p => p.AjustarEstoqueAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_EstoqueFicariaNegativo_PropagaRegraDeNegocioEDesfaz()
    {
        _produtos.Setup(p => p.AjustarEstoqueAsync(7, -10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RegraDeNegocioException("fora do intervalo"));

        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => CriarCasoDeUso().ExecutarAsync(new AjustarEstoqueProdutoEntrada(7, -10), CancellationToken.None));

        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ProdutoInexistente_PropagaNaoEncontrado()
    {
        _produtos.Setup(p => p.AjustarEstoqueAsync(999, 1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NaoEncontradoException("Produto", 999));

        await Assert.ThrowsAsync<NaoEncontradoException>(
            () => CriarCasoDeUso().ExecutarAsync(new AjustarEstoqueProdutoEntrada(999, 1), CancellationToken.None));
    }
}
