using Moq;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Clientes.AtualizarCliente;
using Pedidos.Domain.Entidades;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.UnitTests.CasosDeUso.Clientes;

/// <summary>Requisito: Atualizar Cliente.</summary>
public class AtualizarClienteUseCaseTests
{
    private static readonly DateTimeOffset CriadoEm = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Id = Guid.CreateVersion7();

    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public AtualizarClienteUseCaseTests() =>
        _clientes.Setup(c => c.ObterAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Cliente.Restaurar(Id, "Ana", "ana@x.com", CriadoEm));

    private AtualizarClienteUseCase CriarCasoDeUso() => new(_clientes.Object, _unitOfWork.Object);

    [Fact]
    public async Task ExecutarAsync_DadosValidos_AtualizaEConfirma()
    {
        var saida = await CriarCasoDeUso().ExecutarAsync(new AtualizarClienteEntrada(Id, " Ana Souza ", "ana.souza@x.com"), CancellationToken.None);

        Assert.Equal(new AtualizarClienteSaida(Id, "Ana Souza", "ana.souza@x.com", CriadoEm), saida);
        _clientes.Verify(c => c.AtualizarAsync(It.Is<Cliente>(x => x.Nome == "Ana Souza"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_EmailDeOutroCliente_LancaConflitoEDesfaz()
    {
        _clientes.Setup(c => c.ExisteEmailAsync("bia@x.com", Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflitoException>(
            () => CriarCasoDeUso().ExecutarAsync(new AtualizarClienteEntrada(Id, "Ana", "bia@x.com"), CancellationToken.None));

        _clientes.Verify(c => c.AtualizarAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_MantemProprioEmail_ConsultaIgnorandoOProprioId()
    {
        await CriarCasoDeUso().ExecutarAsync(new AtualizarClienteEntrada(Id, "Ana", "ANA@x.com"), CancellationToken.None);

        _clientes.Verify(c => c.ExisteEmailAsync("ANA@x.com", Id, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ClienteInexistente_LancaNaoEncontrado()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(
            () => CriarCasoDeUso().ExecutarAsync(new AtualizarClienteEntrada(Guid.NewGuid(), "Ana", "ana@x.com"), CancellationToken.None));

        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_NomeInvalido_LancaRegraDeNegocioSemAtualizar()
    {
        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => CriarCasoDeUso().ExecutarAsync(new AtualizarClienteEntrada(Id, "  ", "ana@x.com"), CancellationToken.None));

        _clientes.Verify(c => c.AtualizarAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
