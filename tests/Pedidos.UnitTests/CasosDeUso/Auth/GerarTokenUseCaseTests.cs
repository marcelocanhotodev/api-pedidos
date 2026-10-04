using Moq;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Auth.GerarToken;
using Pedidos.Application.Excecoes;

namespace Pedidos.UnitTests.CasosDeUso.Auth;

/// <summary>Requisito: Emissão de Token.</summary>
public class GerarTokenUseCaseTests
{
    private readonly Mock<IValidadorDeCredenciais> _validador = new();
    private readonly Mock<IEmissorDeToken> _emissor = new();

    private GerarTokenUseCase CriarCasoDeUso() => new(_validador.Object, _emissor.Object);

    [Fact]
    public async Task ExecutarAsync_CredenciaisValidas_RetornaTokenEValidadeEmSegundos()
    {
        _validador.Setup(v => v.Validar("admin", "segredo")).Returns(true);
        _emissor.Setup(e => e.Emitir("admin")).Returns(new TokenEmitido("jwt-gerado", 3600));

        var saida = await CriarCasoDeUso().ExecutarAsync(new GerarTokenEntrada("admin", "segredo"), CancellationToken.None);

        Assert.Equal(new GerarTokenSaida("jwt-gerado", 3600), saida);
    }

    [Fact]
    public async Task ExecutarAsync_CredenciaisInvalidas_LancaCredenciaisInvalidasSemEmitirToken()
    {
        _validador.Setup(v => v.Validar(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        await Assert.ThrowsAsync<CredenciaisInvalidasException>(
            () => CriarCasoDeUso().ExecutarAsync(new GerarTokenEntrada("admin", "errada"), CancellationToken.None));

        _emissor.Verify(e => e.Emitir(It.IsAny<string>()), Times.Never);
    }
}
