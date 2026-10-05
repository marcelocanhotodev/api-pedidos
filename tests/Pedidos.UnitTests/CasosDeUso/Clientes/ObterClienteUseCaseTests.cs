using Moq;
using Pedidos.Application.CasosDeUso.Clientes.ObterCliente;
using Pedidos.Domain.Entidades;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.UnitTests.CasosDeUso.Clientes;

/// <summary>Requisito: Consultar Clientes (obter por id).</summary>
public class ObterClienteUseCaseTests
{
    private readonly Mock<IClienteRepository> _clientes = new();

    [Fact]
    public async Task ExecutarAsync_ClienteExiste_RetornaCliente()
    {
        const int id = 7;
        var criadoEm = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _clientes.Setup(c => c.ObterAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(Cliente.Restaurar(id, "Ana", "ana@x.com", criadoEm));

        var saida = await new ObterClienteUseCase(_clientes.Object).ExecutarAsync(new ObterClienteEntrada(id), CancellationToken.None);

        Assert.Equal(new ObterClienteSaida(id, "Ana", "ana@x.com", criadoEm), saida);
    }

    [Fact]
    public async Task ExecutarAsync_ClienteInexistente_LancaNaoEncontrado()
    {
        var excecao = await Assert.ThrowsAsync<NaoEncontradoException>(
            () => new ObterClienteUseCase(_clientes.Object).ExecutarAsync(new ObterClienteEntrada(999), CancellationToken.None));

        Assert.Equal("Cliente", excecao.Recurso);
    }
}
