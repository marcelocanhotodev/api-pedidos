using Npgsql;
using Testcontainers.PostgreSql;

namespace Pedidos.IntegrationTests.Infra;

/// <summary>Um container PostgreSQL 16 compartilhado pela suíte; cada teste pode criar seu próprio banco vazio.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>Cria um banco vazio com nome único e devolve sua connection string.</summary>
    public async Task<string> CriarBancoVazioAsync()
    {
        var nome = "teste_" + Guid.NewGuid().ToString("N");

        await using (var conexao = new NpgsqlConnection(ConnectionString))
        {
            await conexao.OpenAsync();
            await using var comando = new NpgsqlCommand($"CREATE DATABASE {nome}", conexao);
            await comando.ExecuteNonQueryAsync();
        }

        return new NpgsqlConnectionStringBuilder(ConnectionString) { Database = nome }.ConnectionString;
    }
}

[CollectionDefinition(Nome)]
public sealed class ColecaoPostgres : ICollectionFixture<PostgresFixture>
{
    public const string Nome = "postgres";
}
