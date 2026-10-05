using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests;

/// <summary>Requisitos de produtos: contrato HTTP de ponta a ponta contra PostgreSQL real.</summary>
[Collection(ColecaoPostgres.Nome)]
public sealed class ProdutosTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const int IdInexistente = 999999;
    private static readonly Uri Rota = new("/api/v1/produtos", UriKind.Relative);

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
    public async Task Criar_DadosValidos_Responde201ComLocationESkuNormalizado()
    {
        var resposta = await _api.PostAsJsonAsync(Rota, new { sku = " cnt-azul-01 ", nome = "  Caneta azul  ", preco = 4.9m, estoque = 100 });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var produto = await LerAsync(resposta);
        Assert.Equal($"/api/v1/produtos/{produto.GetProperty("id").GetInt32()}", resposta.Headers.Location?.OriginalString);
        Assert.Equal("CNT-AZUL-01", produto.GetProperty("sku").GetString());
        Assert.Equal("Caneta azul", produto.GetProperty("nome").GetString());
        Assert.Equal(100, produto.GetProperty("estoque").GetInt32());
    }

    [Fact]
    public async Task Criar_Preco49_DevolvidoComo490NoPostENoGet()
    {
        var resposta = await _api.PostAsJsonAsync(Rota, new { sku = "CNT-1", nome = "Caneta", preco = 4.9m, estoque = 1 });

        var noPost = (await LerAsync(resposta)).GetProperty("preco").GetRawText();
        var noGet = (await LerAsync(await _api.GetAsync(resposta.Headers.Location))).GetProperty("preco").GetRawText();
        Assert.Equal("4.90", noPost);
        Assert.Equal("4.90", noGet);
    }

    [Fact]
    public async Task Criar_SkuDuplicadoComOutraCaixaEEspacos_Responde409()
    {
        await CriarAsync("CNT-1");

        var resposta = await _api.PostAsJsonAsync(Rota, new { sku = " cnt-1 ", nome = "Outra", preco = 1m, estoque = 0 });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Theory]
    [InlineData("ABC 1", 1, 0, "sku")]
    [InlineData("ÇÃO-1", 1, 0, "sku")]
    [InlineData("CNT-1", -1, 0, "preco")]
    [InlineData("CNT-1", 10.999, 0, "preco")]
    [InlineData("CNT-1", 10000000000, 0, "preco")]
    [InlineData("CNT-1", 1, 1000001, "estoque")]
    public async Task Criar_CampoInvalido_Responde400IndicandoOCampo(string sku, double preco, int estoque, string campo)
    {
        var resposta = await _api.PostAsJsonAsync(Rota, new { sku, nome = "Caneta", preco = (decimal)preco, estoque });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.True((await LerAsync(resposta)).GetProperty("errors").TryGetProperty(campo, out _), $"esperava erro em {campo}");
    }

    [Fact]
    public async Task Criar_PrecoAusente_Responde400()
    {
        var resposta = await _api.PostAsJsonAsync(Rota, new { sku = "CNT-1", nome = "Caneta", estoque = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Criar_DoisPostsSimultaneosComMesmoSku_UmCriaEOutroConflita()
    {
        for (var rodada = 0; rodada < 5; rodada++)
        {
            var respostas = await Task.WhenAll(
                _api.PostAsJsonAsync(Rota, new { sku = $"corrida-{rodada}", nome = "A", preco = 1m, estoque = 0 }),
                _api.PostAsJsonAsync(Rota, new { sku = $"CORRIDA-{rodada}", nome = "B", preco = 1m, estoque = 0 }));

            Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], respostas.Select(r => r.StatusCode).Order().ToArray());
        }
    }

    // --- Consultar ---

    [Fact]
    public async Task Obter_Inexistente_Responde404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _api.GetAsync(new Uri($"/api/v1/produtos/{IdInexistente}", UriKind.Relative))).StatusCode);
    }

    [Fact]
    public async Task Listar_BuscaPorSkuEmQualquerCaixa_FiltraNomeOuSku()
    {
        await CriarAsync("CNT-AZUL", "Caneta azul");
        await CriarAsync("LAP-01", "Lápis");
        await CriarAsync("BOR-01", "Borracha cnt");

        var pagina = await LerAsync(await _api.GetAsync(new Uri("/api/v1/produtos?busca=cnt", UriKind.Relative)));

        Assert.Equal(["Borracha cnt", "Caneta azul"], Nomes(pagina));
        Assert.Equal(2, pagina.GetProperty("totalItens").GetInt64());
    }

    [Fact]
    public async Task Listar_BuscaComCuringa_ProcuraOCaractereLiteral()
    {
        await CriarAsync("PROMO", "Desconto 100%");
        await CriarAsync("CNT-1", "Caneta");

        var pagina = await LerAsync(await _api.GetAsync(new Uri("/api/v1/produtos?busca=%25", UriKind.Relative)));

        Assert.Equal(["Desconto 100%"], Nomes(pagina));
    }

    [Fact]
    public async Task Listar_NomesRepetidos_CadaUmApareceUmaVezEntrePaginas()
    {
        var ids = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            ids.Add((await CriarAsync($"IGUAL-{i}", "Igual")).GetProperty("id").GetInt32());
        }

        var vistos = new List<int>();
        for (var pagina = 1; pagina <= 3; pagina++)
        {
            var corpo = await LerAsync(await _api.GetAsync(new Uri($"/api/v1/produtos?tamanhoPagina=1&pagina={pagina}", UriKind.Relative)));
            vistos.AddRange(corpo.GetProperty("itens").EnumerateArray().Select(p => p.GetProperty("id").GetInt32()));
        }

        Assert.Equal(ids, vistos);
    }

    // --- Atualizar ---

    [Fact]
    public async Task Atualizar_Preco_MantemSkuEEstoqueEIgnoraCamposExtras()
    {
        var id = (await CriarAsync("CNT-1", estoque: 7)).GetProperty("id").GetInt32();

        var resposta = await _api.PutAsJsonAsync(new Uri($"/api/v1/produtos/{id}", UriKind.Relative),
            new { nome = "Caneta azul", preco = 5.5m, sku = "OUTRO", estoque = 999 });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var produto = await LerAsync(resposta);
        Assert.Equal("5.50", produto.GetProperty("preco").GetRawText());
        Assert.Equal("CNT-1", produto.GetProperty("sku").GetString());
        Assert.Equal(7, produto.GetProperty("estoque").GetInt32());
    }

    [Fact]
    public async Task Atualizar_Inexistente_Responde404()
    {
        var resposta = await _api.PutAsJsonAsync(new Uri($"/api/v1/produtos/{IdInexistente}", UriKind.Relative), new { nome = "X", preco = 1m });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    // --- Ajustar estoque ---

    [Fact]
    public async Task Ajustar_Mais10_Responde200ComEstoque15()
    {
        var id = (await CriarAsync("CNT-1", estoque: 5)).GetProperty("id").GetInt32();

        var resposta = await AjustarAsync(id, 10);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(15, (await LerAsync(resposta)).GetProperty("estoque").GetInt32());
    }

    [Theory]
    [InlineData(5, -10, 5)]
    [InlineData(999995, 10, 999995)]
    public async Task Ajustar_ForaDoIntervalo_Responde422EMantemEstoque(int inicial, int delta, int esperado)
    {
        var id = (await CriarAsync("CNT-1", estoque: inicial)).GetProperty("id").GetInt32();

        var resposta = await AjustarAsync(id, delta);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Equal(esperado, (await ObterAsync(id)).GetProperty("estoque").GetInt32());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000001)]
    public async Task Ajustar_DeltaInvalido_Responde400(int delta)
    {
        var id = (await CriarAsync("CNT-1")).GetProperty("id").GetInt32();

        var resposta = await AjustarAsync(id, delta);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.True((await LerAsync(resposta)).GetProperty("errors").TryGetProperty("delta", out _));
    }

    [Fact]
    public async Task Ajustar_Inexistente_Responde404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await AjustarAsync(IdInexistente, 1)).StatusCode);
    }

    // --- Concorrência ---

    [Fact]
    public async Task Ajustar_DezSimultaneos_TodosOkEEstoqueExato()
    {
        var id = (await CriarAsync("CNT-1")).GetProperty("id").GetInt32();

        var respostas = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => AjustarAsync(id, 1)));

        Assert.All(respostas, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(10, (await ObterAsync(id)).GetProperty("estoque").GetInt32());
    }

    [Fact]
    public async Task Atualizar_SimultaneoAAjustes_NaoPerdeEstoque()
    {
        var id = (await CriarAsync("CNT-1")).GetProperty("id").GetInt32();

        var ajustes = Enumerable.Range(0, 10).Select(_ => AjustarAsync(id, 1));
        var put = _api.PutAsJsonAsync(new Uri($"/api/v1/produtos/{id}", UriKind.Relative), new { nome = "Caneta nova", preco = 9.99m });
        await Task.WhenAll(ajustes.Append(put));

        var produto = await ObterAsync(id);
        Assert.Equal("Caneta nova", produto.GetProperty("nome").GetString());
        Assert.Equal("9.99", produto.GetProperty("preco").GetRawText());
        Assert.Equal(10, produto.GetProperty("estoque").GetInt32());
    }

    // --- Proteção ---

    [Theory]
    [InlineData("GET", "/api/v1/produtos")]
    [InlineData("POST", "/api/v1/produtos")]
    [InlineData("GET", "/api/v1/produtos/1")]
    [InlineData("PUT", "/api/v1/produtos/1")]
    [InlineData("PATCH", "/api/v1/produtos/1/estoque")]
    public async Task Endpoints_SemToken_Respondem401(string metodo, string rota)
    {
        using var anonimo = _factory.CreateClient();
        using var requisicao = new HttpRequestMessage(new HttpMethod(metodo), new Uri(rota, UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.SendAsync(requisicao)).StatusCode);
    }

    private async Task<JsonElement> CriarAsync(string sku, string nome = "Produto", int estoque = 0)
    {
        var resposta = await _api.PostAsJsonAsync(Rota, new { sku, nome, preco = 1m, estoque });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return await LerAsync(resposta);
    }

    private async Task<JsonElement> ObterAsync(int id) =>
        await LerAsync(await _api.GetAsync(new Uri($"/api/v1/produtos/{id}", UriKind.Relative)));

    private Task<HttpResponseMessage> AjustarAsync(int id, int delta) =>
        _api.PatchAsJsonAsync(new Uri($"/api/v1/produtos/{id}/estoque", UriKind.Relative), new { delta });

    private static async Task<JsonElement> LerAsync(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }

    private static string[] Nomes(JsonElement pagina) =>
        pagina.GetProperty("itens").EnumerateArray().Select(p => p.GetProperty("nome").GetString()!).ToArray();
}
