using FluentValidation.TestHelper;
using Pedidos.Api.Endpoints.Clientes;

namespace Pedidos.UnitTests.Validators;

/// <summary>Requisitos: Criar Cliente, Atualizar Cliente e Consultar Clientes (validação de entrada).</summary>
public class ClienteValidatorsTests
{
    [Fact]
    public void Criar_DadosValidos_EhValido()
    {
        new CriarClienteValidator().TestValidate(new CriarClienteRequest { Nome = "Ana", Email = "ana@x.com" })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Criar_EmailMalformado_AcusaEmail()
    {
        new CriarClienteValidator().TestValidate(new CriarClienteRequest { Nome = "Ana", Email = "invalido" })
            .ShouldHaveValidationErrorFor(r => r.Email)
            .WithErrorMessage("email deve ser um e-mail válido com até 200 caracteres.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_NomeEmBranco_AcusaNome(string nome)
    {
        new CriarClienteValidator().TestValidate(new CriarClienteRequest { Nome = nome, Email = "ana@x.com" })
            .ShouldHaveValidationErrorFor(r => r.Nome);
    }

    [Fact]
    public void Criar_NomeComEspacosDentroDoLimite_EhValido()
    {
        new CriarClienteValidator().TestValidate(new CriarClienteRequest { Nome = "  " + new string('a', 150) + "  ", Email = "ana@x.com" })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Atualizar_NomeAcimaDoLimite_AcusaNome()
    {
        new AtualizarClienteValidator().TestValidate(new AtualizarClienteRequest { Id = Guid.NewGuid(), Nome = new string('a', 151), Email = "ana@x.com" })
            .ShouldHaveValidationErrorFor(r => r.Nome);
    }

    [Fact]
    public void Listar_BuscaMuitoLonga_AcusaBusca()
    {
        new ListarClientesValidator().TestValidate(new ListarClientesRequest { Busca = new string('a', 201) })
            .ShouldHaveValidationErrorFor(r => r.Busca);
    }

    [Fact]
    public void Listar_TamanhoPaginaAcimaDoLimite_AcusaTamanhoPagina()
    {
        new ListarClientesValidator().TestValidate(new ListarClientesRequest { TamanhoPagina = 500 })
            .ShouldHaveValidationErrorFor(r => r.TamanhoPagina);
    }
}
