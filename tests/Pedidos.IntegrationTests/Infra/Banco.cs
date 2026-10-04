using Npgsql;

namespace Pedidos.IntegrationTests.Infra;

internal static class Banco
{
    public static async Task<bool> TabelaExisteAsync(string connectionString, string tabela)
    {
        await using var conexao = new NpgsqlConnection(connectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT to_regclass(@tabela) IS NOT NULL", conexao);
        comando.Parameters.AddWithValue("tabela", tabela);
        return (bool)(await comando.ExecuteScalarAsync())!;
    }

    public static async Task<long> ContarAsync(string connectionString, string sql)
    {
        await using var conexao = new NpgsqlConnection(connectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexao);
        return Convert.ToInt64(await comando.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    public static async Task ExecutarAsync(string connectionString, string sql)
    {
        await using var conexao = new NpgsqlConnection(connectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexao);
        await comando.ExecuteNonQueryAsync();
    }
}
