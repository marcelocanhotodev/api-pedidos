using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Pedidos.Application.Abstracoes;
using Pedidos.Domain.Entidades;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests;

/// <summary>Requisitos: Integridade de Produtos no Banco; Consultar Produtos e Ajustar Estoque (SQL do repositório).</summary>
[Collection(ColecaoPostgres.Nome)]
public sealed class ProdutoRepositoryTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    private string _banco = null!;
    private ApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _banco = await postgres.CriarBancoVazioAsync();
        _factory = new ApiFactory(_banco);
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private async Task<T> NoEscopoAsync<T>(Func<IProdutoRepository, IUnitOfWork, Task<T>> acao)
    {
        await using var escopo = _factory.Services.CreateAsyncScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<IProdutoRepository>(), escopo.ServiceProvider.GetRequiredService<IUnitOfWork>());
    }

    private Task<Produto> InserirAsync(string sku, string nome = "Produto", int estoque = 0) =>
        NoEscopoAsync((repositorio, _) => repositorio.InserirAsync(Produto.Criar(sku, nome, 1m, estoque, Agora), CancellationToken.None));

    private Task<Produto> AjustarAsync(int id, int delta) =>
        NoEscopoAsync((repositorio, uow) => uow.EmTransacaoAsync(() => repositorio.AjustarEstoqueAsync(id, delta, CancellationToken.None), CancellationToken.None));

    // --- Esquema ---

    [Fact]
    public async Task Migracao_BancoVazio_CriaTabelaComIdIdentityIndiceEChecks()
    {
        _ = _factory.Services;

        Assert.Equal(1, await Banco.ContarAsync(_banco, """
            SELECT count(*) FROM information_schema.columns
            WHERE table_name = 'produtos' AND column_name = 'id' AND data_type = 'integer' AND identity_generation = 'ALWAYS'
            """));
        Assert.Equal(1, await Banco.ContarAsync(_banco, "SELECT count(*) FROM pg_indexes WHERE tablename = 'produtos' AND indexname = 'ux_produtos_sku' AND indexdef LIKE '%UNIQUE%'"));
        Assert.Equal(2, await Banco.ContarAsync(_banco, "SELECT count(*) FROM pg_constraint WHERE conrelid = 'produtos'::regclass AND conname IN ('ck_produtos_preco', 'ck_produtos_estoque')"));
    }

    [Fact]
    public async Task Check_UpdateDeixandoEstoqueNegativo_EhRecusadoPeloBanco()
    {
        var produto = await InserirAsync("CNT-1", estoque: 5);

        var excecao = await Assert.ThrowsAsync<PostgresException>(() => Banco.ExecutarAsync(_banco, $"UPDATE produtos SET estoque = -1 WHERE id = {produto.Id}"));

        Assert.Equal(PostgresErrorCodes.CheckViolation, excecao.SqlState);
    }

    [Fact]
    public async Task Inserir_SkuRepetidoSemChecagemPrevia_IndiceUnicoViraConflito()
    {
        await InserirAsync("CNT-1");

        await Assert.ThrowsAsync<ConflitoException>(() => InserirAsync(" cnt-1 "));
    }

    // --- Listagem ---

    [Fact]
    public async Task Listar_BuscaComCuringasEOrdemPorNomeEId()
    {
        var a = await InserirAsync("A-1", "Igual");
        var b = await InserirAsync("B-1", "Igual");
        await InserirAsync("C-1", "Desconto 100%");
        await InserirAsync("NOME_X", "Outro");

        var todos = await NoEscopoAsync((r, _) => r.ListarAsync(null, 0, 10, CancellationToken.None));
        var porcento = await NoEscopoAsync((r, _) => r.ListarAsync("%", 0, 10, CancellationToken.None));
        var sublinhado = await NoEscopoAsync((r, _) => r.ListarAsync("_", 0, 10, CancellationToken.None));

        Assert.Equal(["Desconto 100%", "Igual", "Igual", "Outro"], todos.Itens.Select(p => p.Nome));
        Assert.Equal([a.Id, b.Id], todos.Itens.Where(p => p.Nome == "Igual").Select(p => p.Id));
        Assert.Equal(["Desconto 100%"], porcento.Itens.Select(p => p.Nome));
        Assert.Equal(["NOME_X"], sublinhado.Itens.Select(p => p.Sku));
    }

    // --- Ajuste atômico ---

    [Fact]
    public async Task Ajustar_DentroDoIntervalo_DevolveEstoqueResultante()
    {
        var produto = await InserirAsync("CNT-1", estoque: 5);

        Assert.Equal(15, (await AjustarAsync(produto.Id, 10)).Estoque);
        Assert.Equal(0, (await AjustarAsync(produto.Id, -15)).Estoque);
    }

    [Fact]
    public async Task Ajustar_AbaixoDeZeroOuAcimaDoMaximo_LancaRegraDeNegocioSemAlterar()
    {
        var produto = await InserirAsync("CNT-1", estoque: 999_995);

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => AjustarAsync(produto.Id, 10));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => AjustarAsync(produto.Id, -1_000_000));

        Assert.Equal(999_995, (await NoEscopoAsync((r, _) => r.ObterAsync(produto.Id, CancellationToken.None)))!.Estoque);
    }

    [Fact]
    public async Task Ajustar_ProdutoInexistente_LancaNaoEncontrado()
    {
        _ = _factory.Services;

        await Assert.ThrowsAsync<NaoEncontradoException>(() => AjustarAsync(999_999, 1));
    }

    [Fact]
    public async Task Ajustar_DezAjustesSimultaneos_SomaExata()
    {
        var produto = await InserirAsync("CNT-1");

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => AjustarAsync(produto.Id, 1)));

        Assert.Equal(10, (await NoEscopoAsync((r, _) => r.ObterAsync(produto.Id, CancellationToken.None)))!.Estoque);
    }
}
