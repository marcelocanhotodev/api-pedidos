using Moq;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Clientes.ExcluirCliente;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.UnitTests.CasosDeUso.Clientes;

/// <summary>Requisito: Excluir Cliente.</summary>
public class ExcluirClienteUseCaseTests
{
    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ExcluirClienteUseCase CriarCasoDeUso() => new(_clientes.Object, _unitOfWork.Object);

    [Fact]
    public async Task ExecutarAsync_ClienteExiste_ExcluiEConfirma()
    {
        const int id = 7;
        _clientes.Setup(c => c.ExcluirAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var saida = await CriarCasoDeUso().ExecutarAsync(new ExcluirClienteEntrada(id), CancellationToken.None);

        Assert.Same(Vazio.Valor, saida);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ClienteInexistente_LancaNaoEncontradoEDesfaz()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(
            () => CriarCasoDeUso().ExecutarAsync(new ExcluirClienteEntrada(999), CancellationToken.None));

        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
