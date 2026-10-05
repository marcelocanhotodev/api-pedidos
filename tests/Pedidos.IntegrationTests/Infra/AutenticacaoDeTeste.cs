using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pedidos.IntegrationTests.Infra;

internal static class AutenticacaoDeTeste
{
    /// <summary>Cria um cliente HTTP já com <c>Authorization: Bearer</c> obtido em /api/v1/auth/token.</summary>
    public static async Task<HttpClient> CriarClienteAutenticadoAsync(this ApiFactory factory)
    {
        var cliente = factory.CreateClient();
        var resposta = await cliente.PostAsJsonAsync(
            new Uri("/api/v1/auth/token", UriKind.Relative),
            new { usuario = ApiFactory.Usuario, senha = ApiFactory.Senha });
        resposta.EnsureSuccessStatusCode();

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", corpo.RootElement.GetProperty("accessToken").GetString());
        return cliente;
    }
}
