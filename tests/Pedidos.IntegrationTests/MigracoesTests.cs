using Microsoft.Extensions.Logging.Abstractions;
using Pedidos.Infrastructure.Migracoes;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests;

/// <summary>Requisito: Migrações com DbUp.</summary>
[Collection(ColecaoPostgres.Nome)]
public class MigracoesTests(PostgresFixture postgres)
{
    private readonly MigradorDeBanco _migrador = new(NullLogger<MigradorDeBanco>.Instance);

    [Fact]
    public async Task Executar_BancoVazio_AplicaScriptsEmOrdemERegistraEmSchemaversions()
    {
        var banco = await postgres.CriarBancoVazioAsync();

        var executados = _migrador.Executar(banco);

        Assert.Contains(executados, s => s.EndsWith("0000_linha_de_base.sql", StringComparison.Ordinal));
        Assert.Equal(executados.Order(StringComparer.Ordinal), executados);
        Assert.Equal(executados.Count, await Banco.ContarAsync(banco, "SELECT count(*) FROM schemaversions"));
    }

    [Fact]
    public async Task Executar_ReinicioSemNovosScripts_NaoReexecutaNada()
    {
        var banco = await postgres.CriarBancoVazioAsync();
        var primeiraExecucao = _migrador.Executar(banco);

        var segundaExecucao = _migrador.Executar(banco);

        Assert.NotEmpty(primeiraExecucao);
        Assert.Empty(segundaExecucao);
        Assert.Equal(primeiraExecucao.Count, await Banco.ContarAsync(banco, "SELECT count(*) FROM schemaversions"));
    }

    [Fact]
    public async Task Executar_ScriptComErro_DesfazTransacaoDoScriptELancaExcecao()
    {
        var banco = await postgres.CriarBancoVazioAsync();

        var excecao = Assert.Throws<InvalidOperationException>(
            () => _migrador.Executar(banco, typeof(MigracoesTests).Assembly));

        Assert.Contains("0001_script_com_erro.sql", excecao.Message, StringComparison.Ordinal);
        Assert.False(await Banco.TabelaExisteAsync(banco, "tabela_parcial"));
    }

    [Fact]
    public async Task Startup_AplicarMigracoesTrue_AplicaMigracoesAntesDeAtender()
    {
        var banco = await postgres.CriarBancoVazioAsync();
        await using var factory = new ApiFactory(banco, aplicarMigracoes: true);
        using var cliente = factory.CreateClient();

        await cliente.GetAsync(new Uri("/health/live", UriKind.Relative));

        Assert.True(await Banco.TabelaExisteAsync(banco, "schemaversions"));
    }

    [Fact]
    public async Task Startup_AplicarMigracoesFalse_NaoAlteraEsquema()
    {
        var banco = await postgres.CriarBancoVazioAsync();
        await using var factory = new ApiFactory(banco, aplicarMigracoes: false);
        using var cliente = factory.CreateClient();

        await cliente.GetAsync(new Uri("/health/live", UriKind.Relative));

        Assert.False(await Banco.TabelaExisteAsync(banco, "schemaversions"));
    }
}
