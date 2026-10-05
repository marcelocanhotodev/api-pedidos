using Pedidos.Api.Configuracao;

namespace Pedidos.UnitTests.Api;

/// <summary>Requisito: Informações da API (commit implantado na versão).</summary>
public class InformacoesDaAplicacaoTests
{
    [Fact]
    public void MontarVersao_ComCommit_AcrescentaSeteCaracteres()
    {
        Assert.Equal("1.0.0+0123456", InformacoesDaAplicacao.MontarVersao("1.0.0", "0123456789abcdef"));
    }

    [Fact]
    public void MontarVersao_CommitCurto_UsaInteiro()
    {
        Assert.Equal("1.0.0+abc", InformacoesDaAplicacao.MontarVersao("1.0.0", "abc"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MontarVersao_SemCommit_SoAVersaoDoAssembly(string? commit)
    {
        Assert.Equal("1.0.0", InformacoesDaAplicacao.MontarVersao("1.0.0", commit));
    }
}
