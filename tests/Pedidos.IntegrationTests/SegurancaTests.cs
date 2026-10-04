using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests;

/// <summary>Requisitos: Emissão de Token, Proteção dos Endpoints, Tratamento de Segredos, Versionamento e Documentação da API.</summary>
[Collection(ColecaoPostgres.Nome)]
public sealed class SegurancaTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly Uri RotaToken = new("/api/v1/auth/token", UriKind.Relative);
    private static readonly Uri RotaProtegida = new("/api/v1/teste-protegido", UriKind.Relative);

    private ApiFactory _factory = null!;
    private HttpClient _cliente = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(await postgres.CriarBancoVazioAsync());
        _cliente = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _cliente.Dispose();
        await _factory.DisposeAsync();
    }

    // --- Emissão de Token ---

    [Fact]
    public async Task Token_CredenciaisValidas_Responde200ComJwtEExpiraEmEmSegundos()
    {
        var resposta = await _cliente.PostAsJsonAsync(RotaToken, new { usuario = ApiFactory.Usuario, senha = ApiFactory.Senha });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(corpo.RootElement.GetProperty("accessToken").GetString());
        Assert.Equal(ApiFactory.ExpiraMinutos * 60, corpo.RootElement.GetProperty("expiraEm").GetInt32());
        Assert.Equal(ApiFactory.Emissor, jwt.Issuer);
        Assert.InRange(jwt.ValidTo - jwt.ValidFrom, TimeSpan.FromMinutes(ApiFactory.ExpiraMinutos - 1), TimeSpan.FromMinutes(ApiFactory.ExpiraMinutos));
    }

    [Fact]
    public async Task Token_CredenciaisInvalidas_Responde401ComoProblemDetails()
    {
        var resposta = await _cliente.PostAsJsonAsync(RotaToken, new { usuario = ApiFactory.Usuario, senha = "errada" });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal(401, corpo.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Usuário ou senha inválidos.", corpo.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Token_CamposAusentes_Responde400IndicandoOsCampos()
    {
        var resposta = await _cliente.PostAsJsonAsync(RotaToken, new { });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var erros = corpo.RootElement.GetProperty("errors");
        Assert.True(erros.TryGetProperty("usuario", out _));
        Assert.True(erros.TryGetProperty("senha", out _));
    }

    // --- Versionamento ---

    [Fact]
    public async Task Token_RotaSemVersao_Responde404()
    {
        var resposta = await _cliente.PostAsJsonAsync(new Uri("/auth/token", UriKind.Relative), new { usuario = "a", senha = "b" });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    // --- Proteção dos Endpoints ---

    [Fact]
    public async Task Protegido_SemToken_Responde401ComoProblemJson()
    {
        var resposta = await _cliente.GetAsync(RotaProtegida);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Protegido_TokenValido_ProcessaRequisicao()
    {
        var token = await ObterTokenAsync();

        var resposta = await GetComTokenAsync(RotaProtegida, token);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Protegido_TokenExpirado_Responde401()
    {
        // Expirou há 10 segundos: com a tolerância padrão de 5 minutos seria aceito.
        var agora = DateTime.UtcNow;
        var token = CriarToken(ApiFactory.ChaveJwt, notBefore: agora.AddMinutes(-5), expira: agora.AddSeconds(-10));

        var resposta = await GetComTokenAsync(RotaProtegida, token);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Protegido_TokenComAssinaturaInvalida_Responde401()
    {
        var token = CriarToken("outra-chave-qualquer-com-mais-de-32-caracteres", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5));

        var resposta = await GetComTokenAsync(RotaProtegida, token);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Protegido_TokenAdulterado_Responde401()
    {
        var partes = (await ObterTokenAsync()).Split('.');
        var cargaAdulterada = Base64UrlEncoder.Encode("""{"sub":"invasor","iss":"pedidos-api-testes","aud":"pedidos-clientes-testes","exp":4102444800}""");

        var resposta = await GetComTokenAsync(RotaProtegida, $"{partes[0]}.{cargaAdulterada}.{partes[2]}");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Theory]
    [InlineData("/info")]
    [InlineData("/health/ready")]
    [InlineData("/health/live")]
    public async Task RotasPublicas_SemToken_NaoExigemAutenticacao(string rota)
    {
        var resposta = await _cliente.GetAsync(new Uri(rota, UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    // --- Regras sobre as rotas registradas ---

    [Fact]
    public void RotasRegistradas_NenhumaRotaApiV1AlemDoTokenAceitaAnonimo()
    {
        var rotas = RegrasDeRotas.Ler(_factory.Services.GetRequiredService<EndpointDataSource>());

        Assert.Contains(rotas, r => r.Caminho == RegrasDeRotas.RotaDoToken && r.Anonima);
        Assert.Empty(RegrasDeRotas.AnonimasIndevidas(rotas));
    }

    [Fact]
    public void RotasRegistradas_EndpointsDeNegocioEstaoSobApiV1()
    {
        var rotas = RegrasDeRotas.Ler(_factory.Services.GetRequiredService<EndpointDataSource>());

        Assert.Contains(rotas, r => r.Caminho == "/info");
        Assert.Empty(RegrasDeRotas.ForaDoGrupoApiV1(rotas));
    }

    // --- Documentação da API ---

    [Fact]
    public async Task Swagger_EndpointsProtegidosDeclaramBearerETokenEInfoNao()
    {
        using var documento = JsonDocument.Parse(await _cliente.GetStringAsync(new Uri("/swagger/v1/swagger.json", UriKind.Relative)));
        var raiz = documento.RootElement;
        var caminhos = raiz.GetProperty("paths");

        Assert.True(raiz.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("JWTBearerAuth", out _));
        Assert.True(TemSeguranca(caminhos.GetProperty("/api/v1/teste-protegido").GetProperty("get")));
        Assert.False(TemSeguranca(caminhos.GetProperty("/api/v1/auth/token").GetProperty("post")));
        Assert.False(TemSeguranca(caminhos.GetProperty("/info").GetProperty("get")));
    }

    private static bool TemSeguranca(JsonElement operacao) =>
        operacao.TryGetProperty("security", out var seguranca) && seguranca.GetArrayLength() > 0;

    private async Task<string> ObterTokenAsync()
    {
        var resposta = await _cliente.PostAsJsonAsync(RotaToken, new { usuario = ApiFactory.Usuario, senha = ApiFactory.Senha });
        resposta.EnsureSuccessStatusCode();
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return corpo.RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<HttpResponseMessage> GetComTokenAsync(Uri rota, string token)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, rota);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _cliente.SendAsync(requisicao);
    }

    private static string CriarToken(string chave, DateTime notBefore, DateTime expira) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = ApiFactory.Emissor,
            Audience = ApiFactory.Audiencia,
            NotBefore = notBefore,
            IssuedAt = notBefore,
            Expires = expira,
            Subject = new ClaimsIdentity([new Claim("sub", ApiFactory.Usuario)]),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)), SecurityAlgorithms.HmacSha256),
        });
}

/// <summary>Regras de rotas aplicadas a dados conhecidos: provam que as violações são detectadas.</summary>
public class RegrasDeRotasTests
{
    private sealed class EndpointDeNegocioFicticio;

    [Fact]
    public void AnonimasIndevidas_RotaApiV1AnonimaAlemDoToken_DetectaViolacao()
    {
        RotaRegistrada[] rotas =
        [
            new(RegrasDeRotas.RotaDoToken, typeof(object), Anonima: true),
            new("/api/v1/clientes", typeof(object), Anonima: true),
            new("/info", typeof(object), Anonima: true),
        ];

        var violacoes = RegrasDeRotas.AnonimasIndevidas(rotas);

        Assert.Single(violacoes);
        Assert.Contains("/api/v1/clientes", violacoes[0], StringComparison.Ordinal);
    }

    [Fact]
    public void AnonimasIndevidas_SomenteTokenAnonimo_NaoAcusa()
    {
        RotaRegistrada[] rotas =
        [
            new(RegrasDeRotas.RotaDoToken, typeof(object), Anonima: true),
            new("/api/v1/clientes", typeof(object), Anonima: false),
        ];

        Assert.Empty(RegrasDeRotas.AnonimasIndevidas(rotas));
    }

    [Fact]
    public void ForaDoGrupoApiV1_EndpointDeNegocioSemGrupo_DetectaViolacao()
    {
        // Simula um endpoint em Pedidos.Api.Endpoints.* (fora de Sistema) registrado sem o prefixo.
        var tipo = typeof(Pedidos.Api.Endpoints.Auth.GerarTokenRequest);
        RotaRegistrada[] rotas = [new("/auth/token", tipo, Anonima: true)];

        Assert.Single(RegrasDeRotas.ForaDoGrupoApiV1(rotas));
    }

    [Fact]
    public void ForaDoGrupoApiV1_EndpointDeNegocioNoGrupoOuDeSistema_NaoAcusa()
    {
        RotaRegistrada[] rotas =
        [
            new("/api/v1/auth/token", typeof(Pedidos.Api.Endpoints.Auth.GerarTokenRequest), Anonima: true),
            new("/info", typeof(Pedidos.Api.Endpoints.Sistema.ObterInformacoesResponse), Anonima: true),
            new("/teste/erros/x", typeof(EndpointDeNegocioFicticio), Anonima: true),
        ];

        Assert.Empty(RegrasDeRotas.ForaDoGrupoApiV1(rotas));
    }
}

/// <summary>Requisito: Tratamento de Segredos (falha rápida na inicialização).</summary>
[Collection(ColecaoPostgres.Nome)]
public class InicializacaoSemChaveTests(PostgresFixture postgres)
{
    [Theory]
    [InlineData(null)]
    [InlineData("curta")]
    public async Task Startup_ChaveJwtAusenteOuCurta_NaoSobe(string? chave)
    {
        await using var factory = new ApiFactory(await postgres.CriarBancoVazioAsync(), jwtChave: chave);

        Assert.ThrowsAny<Exception>(() => factory.CreateClient());
    }
}
