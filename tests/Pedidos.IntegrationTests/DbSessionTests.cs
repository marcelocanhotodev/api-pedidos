using Microsoft.Extensions.DependencyInjection;
using Pedidos.Infrastructure.Dados;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests;

/// <summary>Requisito: Injeção de Dependência (escopo por requisição descartado de forma síncrona ou assíncrona).</summary>
[Collection(ColecaoPostgres.Nome)]
public class DbSessionTests(PostgresFixture postgres)
{
    [Fact]
    public async Task EscopoSincrono_ComSessaoAberta_DescartaSemErro()
    {
        await using var factory = new ApiFactory(await postgres.CriarBancoVazioAsync());

        var escopo = factory.Services.CreateScope();
        var conexao = await escopo.ServiceProvider.GetRequiredService<IDbSession>().ObterConexaoAsync(CancellationToken.None);
        var excecao = Record.Exception(escopo.Dispose);

        Assert.Null(excecao);
        Assert.Equal(System.Data.ConnectionState.Closed, conexao.State);
    }
}
