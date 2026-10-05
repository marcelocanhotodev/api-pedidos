using Pedidos.Domain.Entidades;
using Pedidos.Domain.Excecoes;

namespace Pedidos.UnitTests.Dominio;

/// <summary>Requisitos: Criar Produto, Consultar e Atualizar Produtos, Ajustar Estoque (regras da entidade).</summary>
public class ProdutoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    private static Produto CriarValido(string sku = "CNT-1", string nome = "Caneta", decimal preco = 4.90m, int estoque = 10) =>
        Produto.Criar(sku, nome, preco, estoque, Agora);

    // --- Criação e normalização ---

    [Fact]
    public void Criar_DadosValidos_SemIdAteSerPersistido()
    {
        var produto = CriarValido();

        Assert.Equal(0, produto.Id);
        Assert.Equal(10, produto.Estoque);
        Assert.Equal(Agora, produto.CriadoEm);
    }

    [Fact]
    public void Criar_SkuENomeComEspacos_NormalizaSkuParaMaiusculas()
    {
        var produto = CriarValido(sku: " abc-1 ", nome: "  Caneta  ");

        Assert.Equal("ABC-1", produto.Sku);
        Assert.Equal("Caneta", produto.Nome);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("abc_1.x-2")]
    public void Criar_SkuComCaracteresPermitidos_Aceita(string sku)
    {
        Assert.Equal(sku.ToUpperInvariant(), CriarValido(sku: sku).Sku);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABC 1")]
    [InlineData("ÇÃO-1")]
    [InlineData("ABC#1")]
    public void Criar_SkuInvalido_LancaRegraDeNegocio(string sku)
    {
        Assert.Throws<RegraDeNegocioException>(() => CriarValido(sku: sku));
    }

    [Fact]
    public void Criar_SkuNoLimiteEAcima_AceitaERecusa()
    {
        Assert.Equal(50, CriarValido(sku: new string('A', 50)).Sku.Length);
        Assert.Throws<RegraDeNegocioException>(() => CriarValido(sku: new string('A', 51)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_NomeVazio_LancaRegraDeNegocio(string nome)
    {
        Assert.Throws<RegraDeNegocioException>(() => CriarValido(nome: nome));
    }

    [Fact]
    public void Criar_NomeAcimaDoLimite_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(() => CriarValido(nome: new string('a', 151)));
    }

    // --- Preço ---

    [Theory]
    [InlineData("0")]
    [InlineData("4.9")]
    [InlineData("9999999999.99")]
    [InlineData("10.990")]
    public void Criar_PrecoValido_Aceita(string preco)
    {
        Assert.Equal(decimal.Parse(preco, System.Globalization.CultureInfo.InvariantCulture), CriarValido(preco: decimal.Parse(preco, System.Globalization.CultureInfo.InvariantCulture)).Preco);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("10.999")]
    [InlineData("10000000000.00")]
    public void Criar_PrecoInvalido_LancaRegraDeNegocio(string preco)
    {
        Assert.Throws<RegraDeNegocioException>(() => CriarValido(preco: decimal.Parse(preco, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("4.9", "4.90")]
    [InlineData("5", "5.00")]
    [InlineData("10.990", "10.99")]
    public void Criar_Preco_FicaSempreComDuasCasas(string enviado, string esperado)
    {
        var produto = CriarValido(preco: decimal.Parse(enviado, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(esperado, produto.Preco.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    // --- Estoque inicial ---

    [Fact]
    public void Criar_EstoqueNosLimites_AceitaEForaRecusa()
    {
        Assert.Equal(0, CriarValido(estoque: 0).Estoque);
        Assert.Equal(1_000_000, CriarValido(estoque: 1_000_000).Estoque);
        Assert.Throws<RegraDeNegocioException>(() => CriarValido(estoque: -1));
        Assert.Throws<RegraDeNegocioException>(() => CriarValido(estoque: 1_000_001));
    }

    // --- Atualização ---

    [Fact]
    public void Atualizar_NomeEPreco_SubstituiSemMudarSkuNemEstoque()
    {
        var produto = CriarValido();

        produto.Atualizar(" Caneta azul ", 5.5m);

        Assert.Equal("Caneta azul", produto.Nome);
        Assert.Equal("5.50", produto.Preco.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("CNT-1", produto.Sku);
        Assert.Equal(10, produto.Estoque);
    }

    [Fact]
    public void Atualizar_PrecoComTresCasas_LancaEMantemDados()
    {
        var produto = CriarValido();

        Assert.Throws<RegraDeNegocioException>(() => produto.Atualizar("Caneta", 1.234m));
        Assert.Equal(4.90m, produto.Preco);
    }

    // --- Delta ---

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(1_000_000)]
    [InlineData(-1_000_000)]
    public void ValidarDelta_DentroDoLimite_NaoLanca(int delta)
    {
        Produto.ValidarDelta(delta);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1_000_001)]
    [InlineData(-1_000_001)]
    [InlineData(int.MinValue)]
    public void ValidarDelta_ZeroOuForaDoLimite_LancaRegraDeNegocio(int delta)
    {
        Assert.Throws<RegraDeNegocioException>(() => Produto.ValidarDelta(delta));
    }

    [Fact]
    public void Restaurar_PrecoDoBanco_MantemDuasCasas()
    {
        var produto = Produto.Restaurar(7, "CNT-1", "Caneta", 4.9m, 3, Agora);

        Assert.Equal(7, produto.Id);
        Assert.Equal("4.90", produto.Preco.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
