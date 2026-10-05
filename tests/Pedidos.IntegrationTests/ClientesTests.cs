using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests;

/// <summary>Requisitos de clientes: contrato HTTP de ponta a ponta contra PostgreSQL real.</summary>
[Collection(ColecaoPostgres.Nome)]
public sealed class ClientesTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly Uri Rota = new("/api/v1/clientes", UriKind.Relative);

    private ApiFactory _factory = null!;
    private HttpClient _api = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(await postgres.CriarBancoVazioAsync());
        _api = await _factory.CriarClienteAutenticadoAsync();
    }

    public async Task DisposeAsync()
    {
        _api.Dispose();
        await _factory.DisposeAsync();
    }

    // --- Criar ---

    [Fact]
    public async Task Criar_DadosValidos_Responde201ComLocation()
    {
        var resposta = await _api.PostAsJsonAsync(Rota, new { nome = "Ana", email = "ana@x.com" });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var cliente = await LerAsync(resposta);
        var id = cliente.GetProperty("id").GetGuid();
        Assert.Equal($"/api/v1/clientes/{id}", resposta.Headers.Location?.OriginalString);
        Assert.Equal("Ana", cliente.GetProperty("nome").GetString());
        Assert.True(cliente.TryGetProperty("criadoEm", out _));

        var obtido = await _api.GetAsync(resposta.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, obtido.StatusCode);
    }

    [Fact]
    public async Task Criar_EspacosNasPontas_GravaSemEspacos()
    {
        var resposta = await _api.PostAsJsonAsync(Rota, new { nome = "  Ana  ", email = " ana@x.com " });

        var criado = await LerAsync(resposta);
        var obtido = await LerAsync(await _api.GetAsync(resposta.Headers.Location));
        Assert.Equal("Ana", criado.GetProperty("nome").GetString());
        Assert.Equal("ana@x.com", obtido.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Criar_EmailDuplicadoComOutraCaixa_Responde409()
    {
        await CriarAsync("Ana", "ana@x.com");

        var resposta = await _api.PostAsJsonAsync(Rota, new { nome = "Outra Ana", email = "ANA@X.COM" });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Criar_EmailInvalido_Responde400IndicandoEmail()
    {
        var resposta = await _api.PostAsJsonAsync(Rota, new { nome = "Ana", email = "invalido" });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await LerAsync(resposta);
        Assert.True(corpo.GetProperty("errors").TryGetProperty("email", out _));
    }

    [Fact]
    public async Task Criar_DoisPostsSimultaneosComMesmoEmail_UmCriaEOutroConflita()
    {
        // Repetido para expor intermitência: a corrida termina no índice único ou na checagem prévia.
        for (var rodada = 0; rodada < 5; rodada++)
        {
            var email = $"corrida{rodada}@x.com";

            var respostas = await Task.WhenAll(
                _api.PostAsJsonAsync(Rota, new { nome = "A", email = email.ToUpperInvariant() }),
                _api.PostAsJsonAsync(Rota, new { nome = "B", email }));

            var status = respostas.Select(r => r.StatusCode).Order().ToArray();
            Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], status);
        }
    }

    // --- Consultar ---

    [Fact]
    public async Task Obter_IdInexistente_Responde404ProblemDetails()
    {
        var resposta = await _api.GetAsync(new Uri($"/api/v1/clientes/{Guid.NewGuid()}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Listar_SemParametros_UsaEnvelopePadraoOrdenadoPorNome()
    {
        await CriarAsync("Carla", "carla@x.com");
        await CriarAsync("Ana", "ana@x.com");
        await CriarAsync("Bruno", "bruno@x.com");

        var pagina = await LerAsync(await _api.GetAsync(Rota));

        Assert.Equal(1, pagina.GetProperty("pagina").GetInt32());
        Assert.Equal(20, pagina.GetProperty("tamanhoPagina").GetInt32());
        Assert.Equal(3, pagina.GetProperty("totalItens").GetInt64());
        Assert.Equal(1, pagina.GetProperty("totalPaginas").GetInt32());
        Assert.Equal(["Ana", "Bruno", "Carla"], Nomes(pagina));
    }

    [Fact]
    public async Task Listar_Busca_FiltraNomeOuEmailSemDiferenciarCaixa()
    {
        await CriarAsync("Ana Souza", "souza@x.com");
        await CriarAsync("Bruno", "bruno.ANA@x.com");
        await CriarAsync("Carla", "carla@x.com");

        var pagina = await LerAsync(await _api.GetAsync(new Uri("/api/v1/clientes?busca=ana", UriKind.Relative)));

        Assert.Equal(["Ana Souza", "Bruno"], Nomes(pagina));
    }

    [Fact]
    public async Task Listar_BuscaComCuringa_ProcuraOCaractereLiteral()
    {
        await CriarAsync("Promo 50% off", "promo@x.com");
        await CriarAsync("Ana", "ana@x.com");

        var pagina = await LerAsync(await _api.GetAsync(new Uri("/api/v1/clientes?busca=%25", UriKind.Relative)));

        Assert.Equal(["Promo 50% off"], Nomes(pagina));
    }

    [Fact]
    public async Task Listar_NomesRepetidos_CadaUmApareceUmaVezEntrePaginas()
    {
        var ids = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            ids.Add((await CriarAsync("Ana Souza", $"ana{i}@x.com")).GetProperty("id").GetGuid());
        }

        var vistos = new List<Guid>();
        for (var pagina = 1; pagina <= 3; pagina++)
        {
            var corpo = await LerAsync(await _api.GetAsync(new Uri($"/api/v1/clientes?tamanhoPagina=1&pagina={pagina}", UriKind.Relative)));
            vistos.AddRange(corpo.GetProperty("itens").EnumerateArray().Select(c => c.GetProperty("id").GetGuid()));
        }

        Assert.Equal(ids.Order(), vistos.Order());
        Assert.Equal(3, vistos.Distinct().Count());
    }

    [Fact]
    public async Task Listar_PaginaAlemDoFim_MantemTotalCorreto()
    {
        await CriarAsync("Ana", "ana@x.com");

        var pagina = await LerAsync(await _api.GetAsync(new Uri("/api/v1/clientes?pagina=5", UriKind.Relative)));

        Assert.Empty(pagina.GetProperty("itens").EnumerateArray());
        Assert.Equal(1, pagina.GetProperty("totalItens").GetInt64());
    }

    // --- Atualizar ---

    [Fact]
    public async Task Atualizar_DadosValidos_Responde200ComClienteAtualizado()
    {
        var id = (await CriarAsync("Ana", "ana@x.com")).GetProperty("id").GetGuid();

        var resposta = await _api.PutAsJsonAsync(new Uri($"/api/v1/clientes/{id}", UriKind.Relative), new { nome = "Ana Souza", email = "ana.souza@x.com" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("Ana Souza", (await LerAsync(resposta)).GetProperty("nome").GetString());
    }

    [Fact]
    public async Task Atualizar_EmailDeOutroCliente_Responde409()
    {
        await CriarAsync("Bia", "bia@x.com");
        var id = (await CriarAsync("Ana", "ana@x.com")).GetProperty("id").GetGuid();

        var resposta = await _api.PutAsJsonAsync(new Uri($"/api/v1/clientes/{id}", UriKind.Relative), new { nome = "Ana", email = "BIA@x.com" });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_ProprioEmailComOutraCaixa_Responde200()
    {
        var id = (await CriarAsync("Ana", "ana@x.com")).GetProperty("id").GetGuid();

        var resposta = await _api.PutAsJsonAsync(new Uri($"/api/v1/clientes/{id}", UriKind.Relative), new { nome = "Ana", email = "ANA@x.com" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_ClienteInexistente_Responde404()
    {
        var resposta = await _api.PutAsJsonAsync(new Uri($"/api/v1/clientes/{Guid.NewGuid()}", UriKind.Relative), new { nome = "Ana", email = "ana@x.com" });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    // --- Excluir ---

    [Fact]
    public async Task Excluir_ClienteExistente_Responde204EDeixaDeExistir()
    {
        var id = (await CriarAsync("Ana", "ana@x.com")).GetProperty("id").GetGuid();
        var rota = new Uri($"/api/v1/clientes/{id}", UriKind.Relative);

        var resposta = await _api.DeleteAsync(rota);

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _api.GetAsync(rota)).StatusCode);
    }

    [Fact]
    public async Task Excluir_ClienteInexistente_Responde404()
    {
        var resposta = await _api.DeleteAsync(new Uri($"/api/v1/clientes/{Guid.NewGuid()}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    // --- Proteção ---

    [Theory]
    [InlineData("GET", "/api/v1/clientes")]
    [InlineData("POST", "/api/v1/clientes")]
    [InlineData("GET", "/api/v1/clientes/0190f0b0-0000-7000-8000-000000000000")]
    [InlineData("PUT", "/api/v1/clientes/0190f0b0-0000-7000-8000-000000000000")]
    [InlineData("DELETE", "/api/v1/clientes/0190f0b0-0000-7000-8000-000000000000")]
    public async Task Endpoints_SemToken_Respondem401(string metodo, string rota)
    {
        using var anonimo = _factory.CreateClient();
        using var requisicao = new HttpRequestMessage(new HttpMethod(metodo), new Uri(rota, UriKind.Relative));

        var resposta = await anonimo.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    private async Task<JsonElement> CriarAsync(string nome, string email)
    {
        var resposta = await _api.PostAsJsonAsync(Rota, new { nome, email });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return await LerAsync(resposta);
    }

    private static async Task<JsonElement> LerAsync(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }

    private static string[] Nomes(JsonElement pagina) =>
        pagina.GetProperty("itens").EnumerateArray().Select(c => c.GetProperty("nome").GetString()!).ToArray();
}
