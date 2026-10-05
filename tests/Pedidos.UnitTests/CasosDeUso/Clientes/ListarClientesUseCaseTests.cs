using Moq;
using Pedidos.Application.CasosDeUso.Clientes.ListarClientes;
using Pedidos.Application.Comum;
using Pedidos.Domain.Entidades;
using Pedidos.Domain.Repositorios;

namespace Pedidos.UnitTests.CasosDeUso.Clientes;

/// <summary>Requisito: Consultar Clientes (listagem paginada e busca).</summary>
public class ListarClientesUseCaseTests
{
    private static readonly DateTimeOffset CriadoEm = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly Mock<IClienteRepository> _clientes = new();

    [Fact]
    public async Task ExecutarAsync_Pagina2Tamanho2_ConsultaComDeslocamentoEMontaEnvelope()
    {
        var cliente = Cliente.Restaurar(Guid.CreateVersion7(), "Ana", "ana@x.com", CriadoEm);
        _clientes.Setup(c => c.ListarAsync(null, 2, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginaDeClientes([cliente], Total: 5));

        var saida = await new ListarClientesUseCase(_clientes.Object)
            .ExecutarAsync(new ListarClientesEntrada(null, new Paginacao(2, 2)), CancellationToken.None);

        Assert.Equal([new ClienteResumo(cliente.Id, "Ana", "ana@x.com", CriadoEm)], saida.Pagina.Itens);
        Assert.Equal(2, saida.Pagina.Pagina);
        Assert.Equal(5, saida.Pagina.TotalItens);
        Assert.Equal(3, saida.Pagina.TotalPaginas);
    }

    [Theory]
    [InlineData("  ana  ", "ana")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public async Task ExecutarAsync_Busca_RemoveEspacosEIgnoraVazia(string? busca, string? esperada)
    {
        _clientes.Setup(c => c.ListarAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginaDeClientes([], 0));

        await new ListarClientesUseCase(_clientes.Object).ExecutarAsync(new ListarClientesEntrada(busca, new Paginacao()), CancellationToken.None);

        _clientes.Verify(c => c.ListarAsync(esperada, 0, 20, It.IsAny<CancellationToken>()), Times.Once);
    }
}
