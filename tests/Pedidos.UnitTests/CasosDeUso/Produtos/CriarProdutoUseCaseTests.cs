using Moq;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Produtos.CriarProduto;
using Pedidos.Domain.Entidades;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.UnitTests.CasosDeUso.Produtos;

/// <summary>Requisito: Criar Produto.</summary>
public class CriarProdutoUseCaseTests
{
    private const int IdGerado = 42;
    private static readonly DateTimeOffset Agora = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IProdutoRepository> _produtos = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRelogio> _relogio = new();

    public CriarProdutoUseCaseTests()
    {
        _relogio.SetupGet(r => r.AgoraUtc).Returns(Agora);
        _produtos.Setup(p => p.InserirAsync(It.IsAny<Produto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Produto p, CancellationToken _) => Produto.Restaurar(IdGerado, p.Sku, p.Nome, p.Preco, p.Estoque, p.CriadoEm));
    }

    private CriarProdutoUseCase CriarCasoDeUso() => new(_produtos.Object, _unitOfWork.Object, _relogio.Object);

    [Fact]
    public async Task ExecutarAsync_DadosValidos_InsereNormalizadoEDevolveIdGerado()
    {
        var saida = await CriarCasoDeUso().ExecutarAsync(new CriarProdutoEntrada(" cnt-1 ", " Caneta ", 4.9m, 10), CancellationToken.None);

        Assert.Equal(new CriarProdutoSaida(IdGerado, "CNT-1", "Caneta", 4.90m, 10, Agora), saida);
        Assert.Equal("4.90", saida.Preco.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _produtos.Verify(p => p.ExisteSkuAsync("CNT-1", It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_SkuJaExiste_LancaConflitoSemInserir()
    {
        _produtos.Setup(p => p.ExisteSkuAsync("CNT-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflitoException>(
            () => CriarCasoDeUso().ExecutarAsync(new CriarProdutoEntrada("cnt-1", "Caneta", 4.9m, 10), CancellationToken.None));

        _produtos.Verify(p => p.InserirAsync(It.IsAny<Produto>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_DadoInvalido_LancaRegraDeNegocioSemAbrirTransacao()
    {
        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => CriarCasoDeUso().ExecutarAsync(new CriarProdutoEntrada("CNT-1", "Caneta", 10.999m, 10), CancellationToken.None));

        _unitOfWork.Verify(u => u.IniciarAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
