using Npgsql;

namespace Pedidos.Infrastructure.Dados;

/// <summary>
/// Conexão (e transação, quando houver) compartilhada por todos os repositórios da requisição atual.
/// </summary>
public interface IDbSession
{
    /// <summary>Abre a conexão na primeira chamada e a reutiliza nas seguintes.</summary>
    Task<NpgsqlConnection> ObterConexaoAsync(CancellationToken ct);

    /// <summary>Transação ativa, a ser passada aos comandos Dapper; nula fora de uma unidade de trabalho.</summary>
    NpgsqlTransaction? Transacao { get; }
}
