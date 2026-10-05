using FluentValidation.TestHelper;
using Pedidos.Api.Endpoints.Produtos;

namespace Pedidos.UnitTests.Validators;

/// <summary>Requisitos: Criar Produto, Consultar e Atualizar Produtos, Ajustar Estoque (validação de entrada).</summary>
public class ProdutoValidatorsTests
{
    private static CriarProdutoRequest CriarValido() => new() { Sku = "CNT-1", Nome = "Caneta", Preco = 4.90m, Estoque = 10 };

    [Fact]
    public void Criar_DadosValidos_EhValido()
    {
        new CriarProdutoValidator().TestValidate(CriarValido()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("ABC 1")]
    [InlineData("")]
    [InlineData("ÇÃO")]
    public void Criar_SkuInvalido_AcusaSku(string sku)
    {
        new CriarProdutoValidator().TestValidate(new CriarProdutoRequest { Sku = sku, Nome = "Caneta", Preco = 1m })
            .ShouldHaveValidationErrorFor(r => r.Sku);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("10.999")]
    [InlineData("10000000000")]
    public void Criar_PrecoInvalido_AcusaPreco(string preco)
    {
        var request = new CriarProdutoRequest { Sku = "CNT-1", Nome = "Caneta", Preco = decimal.Parse(preco, System.Globalization.CultureInfo.InvariantCulture) };

        new CriarProdutoValidator().TestValidate(request).ShouldHaveValidationErrorFor(r => r.Preco);
    }

    [Fact]
    public void Criar_PrecoAusente_AcusaPreco()
    {
        new CriarProdutoValidator().TestValidate(new CriarProdutoRequest { Sku = "CNT-1", Nome = "Caneta" })
            .ShouldHaveValidationErrorFor(r => r.Preco);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    public void Criar_EstoqueForaDoLimite_AcusaEstoque(int estoque)
    {
        new CriarProdutoValidator().TestValidate(new CriarProdutoRequest { Sku = "CNT-1", Nome = "Caneta", Preco = 1m, Estoque = estoque })
            .ShouldHaveValidationErrorFor(r => r.Estoque);
    }

    [Fact]
    public void Atualizar_NomeVazioEPrecoComTresCasas_AcusaOsDois()
    {
        var resultado = new AtualizarProdutoValidator().TestValidate(new AtualizarProdutoRequest { Id = 1, Nome = " ", Preco = 1.234m });

        resultado.ShouldHaveValidationErrorFor(r => r.Nome);
        resultado.ShouldHaveValidationErrorFor(r => r.Preco);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1_000_001)]
    [InlineData(-1_000_001)]
    public void AjustarEstoque_DeltaInvalido_AcusaDelta(int delta)
    {
        new AjustarEstoqueProdutoValidator().TestValidate(new AjustarEstoqueProdutoRequest { Id = 1, Delta = delta })
            .ShouldHaveValidationErrorFor(r => r.Delta)
            .WithErrorMessage(ProdutoValidacao.MensagemDelta);
    }

    [Fact]
    public void AjustarEstoque_DeltaValido_EhValido()
    {
        new AjustarEstoqueProdutoValidator().TestValidate(new AjustarEstoqueProdutoRequest { Id = 1, Delta = -5 })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Listar_TamanhoPaginaAcimaDoLimite_AcusaTamanhoPagina()
    {
        new ListarProdutosValidator().TestValidate(new ListarProdutosRequest { TamanhoPagina = 500 })
            .ShouldHaveValidationErrorFor(r => r.TamanhoPagina);
    }
}
