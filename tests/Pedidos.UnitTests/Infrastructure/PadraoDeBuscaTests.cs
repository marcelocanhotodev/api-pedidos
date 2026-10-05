using Pedidos.Infrastructure.Dados.Repositorios;

namespace Pedidos.UnitTests.Infrastructure;

/// <summary>Requisito: Consultar Clientes (curingas tratados como literais).</summary>
public class PadraoDeBuscaTests
{
    [Theory]
    [InlineData("ana", "%ana%")]
    [InlineData("  ana  ", "%ana%")]
    [InlineData("%", @"%\%%")]
    [InlineData("_", @"%\_%")]
    [InlineData(@"a\b", @"%a\\b%")]
    [InlineData("50%_off", @"%50\%\_off%")]
    public void PadraoDeBusca_Termo_EscapaCuringas(string busca, string esperado)
    {
        Assert.Equal(esperado, ClienteRepository.PadraoDeBusca(busca));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PadraoDeBusca_SemTermo_RetornaNulo(string? busca)
    {
        Assert.Null(ClienteRepository.PadraoDeBusca(busca));
    }
}
