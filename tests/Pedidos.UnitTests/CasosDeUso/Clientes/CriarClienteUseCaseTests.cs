using Moq;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Clientes.CriarCliente;
using Pedidos.Domain.Entidades;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.UnitTests.CasosDeUso.Clientes;

/// <summary>Requisito: Criar Cliente.</summary>
public class CriarClienteUseCaseTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRelogio> _relogio = new();

    public CriarClienteUseCaseTests()
    {
        _relogio.SetupGet(r => r.AgoraUtc).Returns(Agora);
        // O repositório devolve o cliente persistido, com o id gerado pelo banco.
        _clientes.Setup(c => c.InserirAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cliente c, CancellationToken _) => Cliente.Restaurar(IdGerado, c.Nome, c.Email, c.CriadoEm));
    }

    private const int IdGerado = 42;

    private CriarClienteUseCase CriarCasoDeUso() => new(_clientes.Object, _unitOfWork.Object, _relogio.Object);

    [Fact]
    public async Task ExecutarAsync_DadosValidos_InsereEDevolveIdGeradoPeloBanco()
    {
        var saida = await CriarCasoDeUso().ExecutarAsync(new CriarClienteEntrada("Ana", "ana@x.com"), CancellationToken.None);

        Assert.Equal(new CriarClienteSaida(IdGerado, "Ana", "ana@x.com", Agora), saida);
        _clientes.Verify(c => c.InserirAsync(It.Is<Cliente>(x => x.Id == 0 && x.Nome == "Ana"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_EspacosNasPontas_GravaNormalizado()
    {
        var saida = await CriarCasoDeUso().ExecutarAsync(new CriarClienteEntrada("  Ana  ", " ana@x.com "), CancellationToken.None);

        Assert.Equal("Ana", saida.Nome);
        Assert.Equal("ana@x.com", saida.Email);
        _clientes.Verify(c => c.ExisteEmailAsync("ana@x.com", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_EmailJaExiste_LancaConflitoEDesfazTransacao()
    {
        _clientes.Setup(c => c.ExisteEmailAsync("ana@x.com", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflitoException>(
            () => CriarCasoDeUso().ExecutarAsync(new CriarClienteEntrada("Ana", "ana@x.com"), CancellationToken.None));

        _clientes.Verify(c => c.InserirAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_ConflitoNoIndiceAoInserir_PropagaConflitoEDesfazTransacao()
    {
        _clientes.Setup(c => c.InserirAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflitoException("duplicado"));

        await Assert.ThrowsAsync<ConflitoException>(
            () => CriarCasoDeUso().ExecutarAsync(new CriarClienteEntrada("Ana", "ana@x.com"), CancellationToken.None));

        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_EmailInvalido_LancaRegraDeNegocioSemAbrirTransacao()
    {
        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => CriarCasoDeUso().ExecutarAsync(new CriarClienteEntrada("Ana", "invalido"), CancellationToken.None));

        _unitOfWork.Verify(u => u.IniciarAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
