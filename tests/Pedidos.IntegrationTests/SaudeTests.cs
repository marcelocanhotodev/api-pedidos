using System.Net;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests;

/// <summary>Requisitos: Health Checks; Testes de Integração (aplicação sobe contra banco real).</summary>
[Collection(ColecaoPostgres.Nome)]
public class SaudeTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Ready_BancoDisponivel_Responde200()
    {
        await using var factory = new ApiFactory(await postgres.CriarBancoVazioAsync());
        using var cliente = factory.CreateClient();

        var ready = await cliente.GetAsync(new Uri("/health/ready", UriKind.Relative));
        var live = await cliente.GetAsync(new Uri("/health/live", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }

    [Fact]
    public async Task Ready_BancoIndisponivel_Responde503ELiveContinua200()
    {
        const string BancoInacessivel = "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=2";
        await using var factory = new ApiFactory(BancoInacessivel, aplicarMigracoes: false);
        using var cliente = factory.CreateClient();

        var ready = await cliente.GetAsync(new Uri("/health/ready", UriKind.Relative));
        var live = await cliente.GetAsync(new Uri("/health/live", UriKind.Relative));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }

    [Fact]
    public async Task Swagger_AplicacaoEmExecucao_PublicaInterfaceEDocumentoOpenApi()
    {
        await using var factory = new ApiFactory(await postgres.CriarBancoVazioAsync());
        using var cliente = factory.CreateClient();

        var interfaceSwagger = await cliente.GetAsync(new Uri("/swagger/index.html", UriKind.Relative));
        var documento = await cliente.GetAsync(new Uri("/swagger/v1/swagger.json", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, interfaceSwagger.StatusCode);
        Assert.Equal(HttpStatusCode.OK, documento.StatusCode);
        var openApi = await documento.Content.ReadAsStringAsync();
        Assert.Contains("API de Pedidos", openApi, StringComparison.Ordinal);
        Assert.Contains("\"/info\"", openApi, StringComparison.Ordinal);
        Assert.Contains("Informações da API", openApi, StringComparison.Ordinal);
        Assert.DoesNotContain("/teste/", openApi, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Info_SemAutenticacao_Responde200ComNomeVersaoEAmbiente()
    {
        await using var factory = new ApiFactory(await postgres.CriarBancoVazioAsync());
        using var cliente = factory.CreateClient();

        var resposta = await cliente.GetAsync(new Uri("/info", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var corpo = System.Text.Json.JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal("API de Pedidos", corpo.RootElement.GetProperty("nome").GetString());
        Assert.False(string.IsNullOrEmpty(corpo.RootElement.GetProperty("versao").GetString()));
        Assert.Equal("Testes", corpo.RootElement.GetProperty("ambiente").GetString());
    }
}
