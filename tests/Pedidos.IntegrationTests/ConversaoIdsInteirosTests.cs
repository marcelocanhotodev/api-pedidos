using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Pedidos.Infrastructure.Migracoes;
using Pedidos.IntegrationTests.Infra;

namespace Pedidos.IntegrationTests;

/// <summary>
/// Requisitos: Chaves Primárias Inteiras (conversão sem perda) e Integridade de Clientes no Banco.
/// Simula um banco que já tinha clientes com id uuid (scripts 0000 e 0001) e aplica a 0002.
/// </summary>
[Collection(ColecaoPostgres.Nome)]
public class ConversaoIdsInteirosTests(PostgresFixture postgres)
{
    private static readonly Assembly AssemblyDasMigracoes = typeof(MigradorDeBanco).Assembly;

    [Fact]
    public async Task Migracao0002_ClientesComUuid_PreservaTodosComIdsInteirosNaOrdemDeCadastro()
    {
        var banco = await BancoComClientesUuidAsync();

        new MigradorDeBanco(NullLogger<MigradorDeBanco>.Instance).Executar(banco);

        var clientes = await LerClientesAsync(banco);
        Assert.Equal(
            [(1, "Carla", "carla@x.com"), (2, "Ana", "Ana@x.com"), (3, "Bruno", "bruno@x.com")],
            clientes.Select(c => (c.Id, c.Nome, c.Email)));
        Assert.Equal(
            [new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero)],
            clientes.Select(c => c.CriadoEm));
    }

    [Fact]
    public async Task Migracao0002_BancoConvertido_TemMesmoEsquemaDeUmBancoNovo()
    {
        var convertido = await BancoComClientesUuidAsync();
        new MigradorDeBanco(NullLogger<MigradorDeBanco>.Instance).Executar(convertido);
        var novo = await postgres.CriarBancoVazioAsync();
        new MigradorDeBanco(NullLogger<MigradorDeBanco>.Instance).Executar(novo);

        Assert.Equal(await DescreverEsquemaAsync(novo), await DescreverEsquemaAsync(convertido));
    }

    [Fact]
    public async Task Migracao0002_AposConversao_NovosIdsContinuamASequenciaEEmailSegueUnico()
    {
        var banco = await BancoComClientesUuidAsync();
        new MigradorDeBanco(NullLogger<MigradorDeBanco>.Instance).Executar(banco);

        await Banco.ExecutarAsync(banco, "INSERT INTO clientes (nome, email, criado_em) VALUES ('Duda', 'duda@x.com', now())");
        var duplicado = await Assert.ThrowsAsync<PostgresException>(() => Banco.ExecutarAsync(banco,
            "INSERT INTO clientes (nome, email, criado_em) VALUES ('Outra Ana', 'ANA@X.COM', now())"));

        Assert.Equal(4, (await LerClientesAsync(banco)).Single(c => c.Nome == "Duda").Id);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicado.SqlState);
    }

    /// <summary>Banco novo só com os scripts 0000 e 0001 aplicados e três clientes com id uuid.</summary>
    private async Task<string> BancoComClientesUuidAsync()
    {
        var banco = await postgres.CriarBancoVazioAsync();
        foreach (var script in new[] { "0000_linha_de_base.sql", "0001_criar_clientes.sql" })
        {
            await Banco.ExecutarAsync(banco, LerScript(script));
        }

        await Banco.ExecutarAsync(banco, """
            CREATE TABLE schemaversions (schemaversionsid serial PRIMARY KEY, scriptname varchar(255) NOT NULL, applied timestamp NOT NULL);
            INSERT INTO schemaversions (scriptname, applied) VALUES
              ('Pedidos.Infrastructure.Migracoes.Scripts.0000_linha_de_base.sql', now()),
              ('Pedidos.Infrastructure.Migracoes.Scripts.0001_criar_clientes.sql', now());
            INSERT INTO clientes (id, nome, email, criado_em) VALUES
              (gen_random_uuid(), 'Ana',   'Ana@x.com',   '2026-01-02T00:00:00Z'),
              (gen_random_uuid(), 'Bruno', 'bruno@x.com', '2026-01-03T00:00:00Z'),
              (gen_random_uuid(), 'Carla', 'carla@x.com', '2026-01-01T00:00:00Z');
            """);
        return banco;
    }

    private static string LerScript(string nome)
    {
        var recurso = AssemblyDasMigracoes.GetManifestResourceNames().Single(n => n.EndsWith(nome, StringComparison.Ordinal));
        using var leitor = new StreamReader(AssemblyDasMigracoes.GetManifestResourceStream(recurso)!);
        return leitor.ReadToEnd();
    }

    private static async Task<List<(int Id, string Nome, string Email, DateTimeOffset CriadoEm)>> LerClientesAsync(string banco)
    {
        await using var conexao = Banco.NovaConexao(banco);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT id, nome, email, criado_em FROM clientes ORDER BY id", conexao);
        await using var leitor = await comando.ExecuteReaderAsync();
        var clientes = new List<(int, string, string, DateTimeOffset)>();
        while (await leitor.ReadAsync())
        {
            clientes.Add((leitor.GetInt32(0), leitor.GetString(1), leitor.GetString(2), leitor.GetFieldValue<DateTimeOffset>(3)));
        }

        return clientes;
    }

    /// <summary>Colunas (ordem, tipo, nulidade, identity), constraints e índices da tabela clientes.</summary>
    private static async Task<string> DescreverEsquemaAsync(string banco)
    {
        await using var conexao = Banco.NovaConexao(banco);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand("""
            SELECT string_agg(linha, E'\n' ORDER BY linha) FROM (
              SELECT format('col %s %s %s %s %s %s', ordinal_position, column_name, data_type, is_nullable, is_identity, coalesce(identity_generation, '-')) AS linha
              FROM information_schema.columns WHERE table_name = 'clientes'
              UNION ALL
              SELECT format('con %s %s', conname, pg_get_constraintdef(oid)) FROM pg_constraint WHERE conrelid = 'clientes'::regclass
              UNION ALL
              SELECT format('idx %s', indexdef) FROM pg_indexes WHERE tablename = 'clientes'
              UNION ALL
              SELECT format('seq %s', sequencename) FROM pg_sequences WHERE sequencename LIKE 'clientes%'
            ) partes
            """, conexao);
        return (string)(await comando.ExecuteScalarAsync())!;
    }
}
