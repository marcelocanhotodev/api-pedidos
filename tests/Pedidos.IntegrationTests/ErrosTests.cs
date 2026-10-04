using System.Net;
using System.Text.Json;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests;

/// <summary>Requisito: Formato Padrão de Erros (contrato HTTP de ponta a ponta).</summary>
[Collection(ColecaoPostgres.Nome)]
public sealed class ErrosTests(PostgresFixture postgres) : IAsyncLifetime
{
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

    [Theory]
    [InlineData("nao-encontrado", 404, "Recurso '42' não foi encontrado.")]
    [InlineData("conflito", 409, "Valor já cadastrado.")]
    [InlineData("regra", 422, "Estoque insuficiente.")]
    public async Task ExcecaoDeDominio_RespondeProblemDetailsComStatusMapeado(string tipo, int status, string detalhe)
    {
        var resposta = await _cliente.GetAsync(new Uri($"/teste/erros/{tipo}", UriKind.Relative));

        var corpo = await LerProblemaAsync(resposta);
        Assert.Equal(status, (int)resposta.StatusCode);
        Assert.Equal(status, corpo.GetProperty("status").GetInt32());
        Assert.Equal(detalhe, corpo.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrEmpty(corpo.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrEmpty(corpo.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task ExcecaoInesperada_Responde500GenericoSemStackTrace()
    {
        var resposta = await _cliente.GetAsync(new Uri("/teste/erros/inesperado", UriKind.Relative));

        var texto = await resposta.Content.ReadAsStringAsync();
        var corpo = await LerProblemaAsync(resposta);
        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.Equal(500, corpo.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(corpo.GetProperty("traceId").GetString()));
        Assert.DoesNotContain("senha=123", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", texto, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FalhaDeValidacao_Responde400ComErrosPorCampo()
    {
        using var conteudo = new StringContent("""{"nome":"","email":"invalido"}""", System.Text.Encoding.UTF8, "application/json");

        var resposta = await _cliente.PostAsync(new Uri("/teste/validacao", UriKind.Relative), conteudo);

        var corpo = await LerProblemaAsync(resposta);
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var erros = corpo.GetProperty("errors");
        Assert.Equal("nome é obrigatório.", erros.GetProperty("nome")[0].GetString());
        Assert.Equal("email inválido.", erros.GetProperty("email")[0].GetString());
        Assert.False(string.IsNullOrEmpty(corpo.GetProperty("traceId").GetString()));
    }

    private static async Task<JsonElement> LerProblemaAsync(HttpResponseMessage resposta)
    {
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }
}
