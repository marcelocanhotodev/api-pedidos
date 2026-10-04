using FastEndpoints;
using FluentValidation.TestHelper;
using Pedidos.Api.Comum;

namespace Pedidos.UnitTests.Validators;

/// <summary>Requisito: Paginação (regra de validação compartilhada).</summary>
public class PaginacaoValidacaoTests
{
    private readonly ListagemDeTesteValidator _validator = new();

    [Fact]
    public void Validar_SemParametros_EhValido()
    {
        var resultado = _validator.TestValidate(new ListagemDeTesteRequest());

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Validar_TamanhoPaginaNosLimites_EhValido(int tamanhoPagina)
    {
        var resultado = _validator.TestValidate(new ListagemDeTesteRequest { TamanhoPagina = tamanhoPagina });

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(500)]
    public void Validar_TamanhoPaginaForaDoLimite_AcusaTamanhoPagina(int tamanhoPagina)
    {
        var resultado = _validator.TestValidate(new ListagemDeTesteRequest { TamanhoPagina = tamanhoPagina });

        resultado.ShouldHaveValidationErrorFor(r => r.TamanhoPagina)
            .WithErrorMessage("tamanhoPagina deve estar entre 1 e 100.");
    }

    [Fact]
    public void Validar_PaginaZero_AcusaPagina()
    {
        var resultado = _validator.TestValidate(new ListagemDeTesteRequest { Pagina = 0 });

        resultado.ShouldHaveValidationErrorFor(r => r.Pagina);
    }

    [Fact]
    public void ParaPaginacao_ValoresDoRequest_ConverteParaParametrosDaApplication()
    {
        var paginacao = new ListagemDeTesteRequest { Pagina = 2, TamanhoPagina = 50 }.ParaPaginacao();

        Assert.Equal(2, paginacao.Pagina);
        Assert.Equal(50, paginacao.TamanhoPagina);
    }

    public sealed class ListagemDeTesteRequest : PaginacaoRequest;

    public sealed class ListagemDeTesteValidator : Validator<ListagemDeTesteRequest>
    {
        public ListagemDeTesteValidator() => this.AplicarRegrasDePaginacao();
    }
}
