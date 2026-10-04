using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Pedidos.Application.Abstracoes;
using Pedidos.Infrastructure.Dados;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests;

/// <summary>Requisitos: Unit of Work; Acesso a Dados com Dapper; Injeção de Dependência (escopo por requisição).</summary>
[Collection(ColecaoPostgres.Nome)]
public sealed class UnitOfWorkTests(PostgresFixture postgres) : IAsyncLifetime
{
    private string _banco = null!;
    private ApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _banco = await postgres.CriarBancoVazioAsync();
        await Banco.ExecutarAsync(_banco, "CREATE TABLE itens_teste (id int PRIMARY KEY, criado_em timestamptz NOT NULL DEFAULT now())");
        _factory = new ApiFactory(_banco);
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Commit_CasoDeUsoTerminaComSucesso_ConfirmaTodasAsAlteracoes()
    {
        await using (var escopo = _factory.Services.CreateAsyncScope())
        {
            var (uow, sessao) = Resolver(escopo);
            await uow.IniciarAsync(CancellationToken.None);
            await InserirAsync(sessao, 1);
            await InserirAsync(sessao, 2);
            await uow.CommitAsync(CancellationToken.None);
        }

        Assert.Equal(2, await Banco.ContarAsync(_banco, "SELECT count(*) FROM itens_teste"));
    }

    [Fact]
    public async Task Rollback_FalhaDepoisDeParteDosComandos_NenhumaAlteracaoPersiste()
    {
        await using (var escopo = _factory.Services.CreateAsyncScope())
        {
            var (uow, sessao) = Resolver(escopo);
            await uow.IniciarAsync(CancellationToken.None);
            await InserirAsync(sessao, 10);
            await uow.RollbackAsync(CancellationToken.None);
        }

        Assert.Equal(0, await Banco.ContarAsync(_banco, "SELECT count(*) FROM itens_teste WHERE id = 10"));
    }

    [Fact]
    public async Task Escopo_TransacaoNaoConfirmada_EhDesfeitaAoFimDaRequisicao()
    {
        await using (var escopo = _factory.Services.CreateAsyncScope())
        {
            var (uow, sessao) = Resolver(escopo);
            await uow.IniciarAsync(CancellationToken.None);
            await InserirAsync(sessao, 20);
        }

        Assert.Equal(0, await Banco.ContarAsync(_banco, "SELECT count(*) FROM itens_teste WHERE id = 20"));
    }

    [Fact]
    public async Task Escopo_DuasRequisicoes_UsamConexoesDiferentes()
    {
        await using var escopoA = _factory.Services.CreateAsyncScope();
        await using var escopoB = _factory.Services.CreateAsyncScope();

        var conexaoA = await escopoA.ServiceProvider.GetRequiredService<IDbSession>().ObterConexaoAsync(CancellationToken.None);
        var conexaoA2 = await escopoA.ServiceProvider.GetRequiredService<IDbSession>().ObterConexaoAsync(CancellationToken.None);
        var conexaoB = await escopoB.ServiceProvider.GetRequiredService<IDbSession>().ObterConexaoAsync(CancellationToken.None);

        Assert.Same(conexaoA, conexaoA2);
        Assert.NotSame(conexaoA, conexaoB);
        Assert.NotEqual(conexaoA.ProcessID, conexaoB.ProcessID);
    }

    [Fact]
    public async Task Dapper_ColunaSnakeCase_MapeiaParaPropriedadePascalCase()
    {
        await Banco.ExecutarAsync(_banco, "INSERT INTO itens_teste (id, criado_em) VALUES (99, '2026-01-02T03:04:05Z')");
        await using var escopo = _factory.Services.CreateAsyncScope();
        var conexao = await escopo.ServiceProvider.GetRequiredService<IDbSession>().ObterConexaoAsync(CancellationToken.None);

        var item = await conexao.QuerySingleAsync<ItemTeste>("SELECT id, criado_em FROM itens_teste WHERE id = @Id", new { Id = 99 });

        Assert.Equal(99, item.Id);
        Assert.Equal(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), item.CriadoEm);
    }

    [Fact]
    public async Task Dapper_DateTimeOffsetComFuso_GravaEmUtc()
    {
        await using var escopo = _factory.Services.CreateAsyncScope();
        var conexao = await escopo.ServiceProvider.GetRequiredService<IDbSession>().ObterConexaoAsync(CancellationToken.None);
        var horarioDeBrasilia = new DateTimeOffset(2026, 5, 1, 9, 0, 0, TimeSpan.FromHours(-3));

        await conexao.ExecuteAsync("INSERT INTO itens_teste (id, criado_em) VALUES (100, @CriadoEm)", new { CriadoEm = horarioDeBrasilia });
        var item = await conexao.QuerySingleAsync<ItemTeste>("SELECT id, criado_em FROM itens_teste WHERE id = 100");

        Assert.Equal(new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero), item.CriadoEm);
        Assert.Equal(TimeSpan.Zero, item.CriadoEm.Offset);
    }

    private static (IUnitOfWork Uow, IDbSession Sessao) Resolver(AsyncServiceScope escopo) =>
        (escopo.ServiceProvider.GetRequiredService<IUnitOfWork>(), escopo.ServiceProvider.GetRequiredService<IDbSession>());

    private static async Task InserirAsync(IDbSession sessao, int id)
    {
        var conexao = await sessao.ObterConexaoAsync(CancellationToken.None);
        await conexao.ExecuteAsync("INSERT INTO itens_teste (id) VALUES (@Id)", new { Id = id }, sessao.Transacao);
    }

    private sealed class ItemTeste
    {
        public int Id { get; init; }

        public DateTimeOffset CriadoEm { get; init; }
    }
}
