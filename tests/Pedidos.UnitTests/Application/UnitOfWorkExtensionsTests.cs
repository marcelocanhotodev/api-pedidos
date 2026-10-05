using Moq;
using Pedidos.Application.Abstracoes;

namespace Pedidos.UnitTests.Application;

/// <summary>Requisito: Regras dos Casos de Uso (escrita controlada por IUnitOfWork).</summary>
public class UnitOfWorkExtensionsTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private readonly MockSequence _sequencia = new();

    [Fact]
    public async Task EmTransacaoAsync_OperacaoComSucesso_IniciaEConfirma()
    {
        _unitOfWork.InSequence(_sequencia).Setup(u => u.IniciarAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.InSequence(_sequencia).Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var resultado = await _unitOfWork.Object.EmTransacaoAsync(() => Task.FromResult(42), CancellationToken.None);

        Assert.Equal(42, resultado);
        _unitOfWork.VerifyAll();
    }

    [Fact]
    public async Task EmTransacaoAsync_OperacaoFalha_DesfazEPropaga()
    {
        _unitOfWork.InSequence(_sequencia).Setup(u => u.IniciarAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.InSequence(_sequencia).Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _unitOfWork.Object.EmTransacaoAsync<int>(() => throw new InvalidOperationException(), CancellationToken.None));

        _unitOfWork.VerifyAll();
    }
}
