using Microsoft.Extensions.DependencyInjection;
using Pedidos.Domain.Entidades;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests;

/// <summary>Requisitos: Integridade de Clientes no Banco; Consultar Clientes (SQL do repositório).</summary>
[Collection(ColecaoPostgres.Nome)]
public sealed class ClienteRepositoryTests(PostgresFixture postgres) : IAsyncLifetime
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

    [Fact]
    public async Task Migracao_BancoVazio_CriaTabelaEIndiceUnicoDeEmail()
    {
        _ = _factory.Services; // sobe a aplicação e aplica as migrações

        Assert.True(await Banco.TabelaExisteAsync(_banco, "clientes"));
        Assert.Equal(1, await Banco.ContarAsync(_banco,
            "SELECT count(*) FROM pg_indexes WHERE tablename = 'clientes' AND indexname = 'ux_clientes_email' AND indexdef LIKE '%UNIQUE%lower%'"));
    }

    [Fact]
    public async Task Inserir_EmailRepetidoComOutraCaixaSemChecagemPrevia_IndiceUnicoViraConflito()
    {
        await using var escopo = _factory.Services.CreateAsyncScope();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IClienteRepository>();
        await repositorio.InserirAsync(Cliente.Criar("Ana", "Ana@x.com", Agora), CancellationToken.None);

        var excecao = await Assert.ThrowsAsync<ConflitoException>(
            () => repositorio.InserirAsync(Cliente.Criar("Outra", "ana@X.com", Agora), CancellationToken.None));

        Assert.Contains("ana@X.com", excecao.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Listar_BuscaComCuringas_TrataComoLiterais()
    {
        await using var escopo = _factory.Services.CreateAsyncScope();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IClienteRepository>();
        await repositorio.InserirAsync(Cliente.Criar("Desconto 100%", "a@x.com", Agora), CancellationToken.None);
        await repositorio.InserirAsync(Cliente.Criar("nome_com_sublinhado", "b@x.com", Agora), CancellationToken.None);
        await repositorio.InserirAsync(Cliente.Criar("Comum", "c@x.com", Agora), CancellationToken.None);

        var porcento = await repositorio.ListarAsync("%", 0, 10, CancellationToken.None);
        var sublinhado = await repositorio.ListarAsync("_", 0, 10, CancellationToken.None);

        Assert.Equal(["Desconto 100%"], porcento.Itens.Select(c => c.Nome));
        Assert.Equal(["nome_com_sublinhado"], sublinhado.Itens.Select(c => c.Nome));
    }

    [Fact]
    public async Task Listar_NomesIguais_DesempataPorId()
    {
        await using var escopo = _factory.Services.CreateAsyncScope();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IClienteRepository>();
        var ids = new List<int>();
        for (var i = 0; i < 4; i++)
        {
            ids.Add((await repositorio.InserirAsync(Cliente.Criar("Igual", $"igual{i}@x.com", Agora), CancellationToken.None)).Id);
        }

        var pagina = await repositorio.ListarAsync(null, 0, 10, CancellationToken.None);

        Assert.Equal(ids.Order(), pagina.Itens.Select(c => c.Id));
        Assert.Equal(4, pagina.Total);
    }

    [Fact]
    public async Task ExisteEmail_IgnorandoOProprioId_NaoAcusaOProprioCliente()
    {
        await using var escopo = _factory.Services.CreateAsyncScope();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IClienteRepository>();
        var cliente = await repositorio.InserirAsync(Cliente.Criar("Ana", "ana@x.com", Agora), CancellationToken.None);

        Assert.True(await repositorio.ExisteEmailAsync("ANA@x.com", null, CancellationToken.None));
        Assert.False(await repositorio.ExisteEmailAsync("ANA@x.com", cliente.Id, CancellationToken.None));
    }

    // --- Chaves primárias inteiras ---

    [Fact]
    public async Task Inserir_DoisClientes_DevolveIdsInteirosCrescentesGeradosPeloBanco()
    {
        await using var escopo = _factory.Services.CreateAsyncScope();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IClienteRepository>();

        var primeiro = await repositorio.InserirAsync(Cliente.Criar("Ana", "ana@x.com", Agora), CancellationToken.None);
        var segundo = await repositorio.InserirAsync(Cliente.Criar("Bia", "bia@x.com", Agora), CancellationToken.None);

        Assert.True(primeiro.Id > 0);
        Assert.True(segundo.Id > primeiro.Id);
        Assert.Equal("Bia", (await repositorio.ObterAsync(segundo.Id, CancellationToken.None))!.Nome);
    }

    [Fact]
    public async Task Esquema_IdDeClientes_EhIntegerIdentityAlways()
    {
        _ = _factory.Services;

        Assert.Equal(1, await Banco.ContarAsync(_banco, """
            SELECT count(*) FROM information_schema.columns
            WHERE table_name = 'clientes' AND column_name = 'id'
              AND data_type = 'integer' AND is_identity = 'YES' AND identity_generation = 'ALWAYS'
            """));
    }

    [Fact]
    public async Task Esquema_InsertInformandoId_EhRecusadoPeloBanco()
    {
        _ = _factory.Services;

        var excecao = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => Banco.ExecutarAsync(_banco,
            "INSERT INTO clientes (id, nome, email, criado_em) VALUES (12345, 'Ana', 'ana@x.com', now())"));

        Assert.Equal("428C9", excecao.SqlState); // generated_always
    }
}
