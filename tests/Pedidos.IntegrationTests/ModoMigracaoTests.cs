using System.Net;
using System.Text.Json;
using Pedidos.Api.Configuracao;
using Pedidos.IntegrationTests.Infra;
using Serilog;

namespace Pedidos.IntegrationTests;

/// <summary>
/// Requisitos: Migração como Etapa de Implantação (modo --migrar) e Informações da API (commit na versão).
/// O modo --migrar é exercitado pelo mesmo ponto de entrada que o Program.cs usa.
/// </summary>
[Collection(ColecaoPostgres.Nome)]
public class ModoMigracaoTests(PostgresFixture postgres)
{
    private static readonly ILogger LogDeFalha = new LoggerConfiguration().CreateLogger();

    // Só a connection string: sem JWT_CHAVE nem AUTH_*, que o modo --migrar não exige.
    private static string[] Argumentos(string banco) => [ModoMigracao.Argumento, $"--ConnectionStrings:Padrao={banco}"];

    [Fact]
    public async Task Migrar_BancoVazio_AplicaTudoESaiComZeroSemConfiguracaoDeSeguranca()
    {
        var banco = await postgres.CriarBancoVazioAsync();

        var codigo = await ModoMigracao.ExecutarAsync(Argumentos(banco), LogDeFalha);

        Assert.Equal(0, codigo);
        Assert.True(await Banco.TabelaExisteAsync(banco, "clientes"));
        Assert.True(await Banco.TabelaExisteAsync(banco, "produtos"));
    }

    [Fact]
    public async Task Migrar_BancoAtualizado_NadaExecutaESaiComZero()
    {
        var banco = await postgres.CriarBancoVazioAsync();
        await ModoMigracao.ExecutarAsync(Argumentos(banco), LogDeFalha);
        var antes = await Banco.ContarAsync(banco, "SELECT count(*) FROM schemaversions");

        var codigo = await ModoMigracao.ExecutarAsync(Argumentos(banco), LogDeFalha);

        Assert.Equal(0, codigo);
        Assert.Equal(antes, await Banco.ContarAsync(banco, "SELECT count(*) FROM schemaversions"));
    }

    [Fact]
    public async Task Migrar_BancoInacessivel_SaiComCodigoDiferenteDeZero()
    {
        var codigo = await ModoMigracao.ExecutarAsync(
            Argumentos("Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=1"), LogDeFalha);

        Assert.Equal(1, codigo);
    }

    [Fact]
    public void FoiSolicitado_SoComOArgumentoExato()
    {
        Assert.True(ModoMigracao.FoiSolicitado(["--migrar"]));
        Assert.False(ModoMigracao.FoiSolicitado(["--migrar=true"]));
        Assert.False(ModoMigracao.FoiSolicitado([]));
    }

    [Fact]
    public async Task Info_ComRenderGitCommit_InformaVersaoComCommitCurto()
    {
        await using var factory = new ApiFactory(
            await postgres.CriarBancoVazioAsync(),
            configuracoesExtras: new Dictionary<string, string> { [InformacoesDaAplicacao.VariavelCommitRender] = "0123456789abcdef" });
        using var cliente = factory.CreateClient();

        var resposta = await cliente.GetAsync(new Uri("/info", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.EndsWith("+0123456", corpo.RootElement.GetProperty("versao").GetString(), StringComparison.Ordinal);
    }
}
