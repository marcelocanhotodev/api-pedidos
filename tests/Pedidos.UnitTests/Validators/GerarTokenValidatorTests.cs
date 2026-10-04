using FluentValidation.TestHelper;
using Pedidos.Api.Endpoints.Auth;

namespace Pedidos.UnitTests.Validators;

/// <summary>Requisito: Emissão de Token (campos ausentes).</summary>
public class GerarTokenValidatorTests
{
    private readonly GerarTokenValidator _validator = new();

    [Fact]
    public void Validar_UsuarioESenhaInformados_EhValido()
    {
        _validator.TestValidate(new GerarTokenRequest { Usuario = "admin", Senha = "x" }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validar_UsuarioAusente_AcusaUsuario()
    {
        _validator.TestValidate(new GerarTokenRequest { Senha = "x" })
            .ShouldHaveValidationErrorFor(r => r.Usuario)
            .WithErrorMessage("usuario é obrigatório.");
    }

    [Fact]
    public void Validar_SenhaAusente_AcusaSenha()
    {
        _validator.TestValidate(new GerarTokenRequest { Usuario = "admin" })
            .ShouldHaveValidationErrorFor(r => r.Senha)
            .WithErrorMessage("senha é obrigatória.");
    }
}
