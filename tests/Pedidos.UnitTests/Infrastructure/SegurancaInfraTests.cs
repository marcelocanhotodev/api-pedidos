using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;
using Pedidos.Application.Abstracoes;
using Pedidos.Infrastructure.Seguranca;

namespace Pedidos.UnitTests.Infrastructure;

/// <summary>Requisitos: Tratamento de Segredos; Emissão de Token.</summary>
public class SegurancaInfraTests
{
    private const string ChaveValida = "chave-de-teste-com-mais-de-32-caracteres!";

    private static Dictionary<string, string?> ConfiguracaoCompleta() => new()
    {
        ["JWT_CHAVE"] = ChaveValida,
        ["JWT_EMISSOR"] = "pedidos-api",
        ["JWT_AUDIENCIA"] = "pedidos-clientes",
        ["JWT_EXPIRA_MINUTOS"] = "60",
        ["AUTH_USUARIO"] = "admin",
        ["AUTH_SENHA"] = "segredo",
    };

    private static OpcoesDeSeguranca Carregar(Dictionary<string, string?> valores) =>
        OpcoesDeSeguranca.Carregar(new ConfigurationBuilder().AddInMemoryCollection(valores).Build());

    // --- Tratamento de Segredos ---

    [Fact]
    public void Carregar_ConfiguracaoCompleta_LeTodasAsVariaveis()
    {
        var opcoes = Carregar(ConfiguracaoCompleta());

        Assert.Equal(ChaveValida, opcoes.Chave);
        Assert.Equal("pedidos-api", opcoes.Emissor);
        Assert.Equal("pedidos-clientes", opcoes.Audiencia);
        Assert.Equal(60, opcoes.ExpiraMinutos);
        Assert.Equal("admin", opcoes.Usuario);
    }

    [Fact]
    public void Carregar_SemChave_FalhaComMensagemClara()
    {
        var valores = ConfiguracaoCompleta();
        valores.Remove("JWT_CHAVE");

        var excecao = Assert.Throws<InvalidOperationException>(() => Carregar(valores));

        Assert.Contains("JWT_CHAVE não foi definida", excecao.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Carregar_ChaveCurta_FalhaComMensagemClara()
    {
        var valores = ConfiguracaoCompleta();
        valores["JWT_CHAVE"] = new string('x', 31);

        var excecao = Assert.Throws<InvalidOperationException>(() => Carregar(valores));

        Assert.Contains("no mínimo 32 caracteres", excecao.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("JWT_EMISSOR")]
    [InlineData("JWT_AUDIENCIA")]
    [InlineData("AUTH_USUARIO")]
    [InlineData("AUTH_SENHA")]
    public void Carregar_VariavelObrigatoriaAusente_FalhaIndicandoAVariavel(string variavel)
    {
        var valores = ConfiguracaoCompleta();
        valores.Remove(variavel);

        var excecao = Assert.Throws<InvalidOperationException>(() => Carregar(valores));

        Assert.Contains(variavel, excecao.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("abc")]
    public void Carregar_ExpiracaoInvalida_Falha(string valor)
    {
        var valores = ConfiguracaoCompleta();
        valores["JWT_EXPIRA_MINUTOS"] = valor;

        Assert.Throws<InvalidOperationException>(() => Carregar(valores));
    }

    // --- Validação de credenciais ---

    [Theory]
    [InlineData("admin", "segredo", true)]
    [InlineData("admin", "errada", false)]
    [InlineData("outro", "segredo", false)]
    [InlineData("admin", "segredo-bem-mais-longo-que-o-original", false)]
    [InlineData("", "", false)]
    public void Validar_Credenciais_ConfereUsuarioESenha(string usuario, string senha, bool esperado)
    {
        var validador = new ValidadorDeCredenciais(Carregar(ConfiguracaoCompleta()));

        Assert.Equal(esperado, validador.Validar(usuario, senha));
    }

    // --- Emissão do JWT ---

    [Fact]
    public void Emitir_Usuario_GeraJwtComEmissorAudienciaEExpiracaoConfigurados()
    {
        var agora = new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
        var relogio = new Mock<IRelogio>();
        relogio.SetupGet(r => r.AgoraUtc).Returns(agora);
        var emissor = new EmissorDeTokenJwt(Carregar(ConfiguracaoCompleta()), relogio.Object);

        var token = emissor.Emitir("admin");

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token.AccessToken);
        Assert.Equal(3600, token.ExpiraEmSegundos);
        Assert.Equal("pedidos-api", jwt.Issuer);
        Assert.Contains("pedidos-clientes", jwt.Audiences);
        Assert.Equal(agora.AddMinutes(60).UtcDateTime, jwt.ValidTo);
        Assert.Equal("admin", jwt.Subject);
        Assert.Equal("HS256", jwt.Alg);
    }
}
