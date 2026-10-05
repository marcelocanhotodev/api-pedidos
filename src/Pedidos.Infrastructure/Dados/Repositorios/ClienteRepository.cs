using Dapper;
using Npgsql;
using Pedidos.Domain.Entidades;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.Infrastructure.Dados.Repositorios;

internal sealed class ClienteRepository(IDbSession sessao) : IClienteRepository
{
    private const string IndiceEmailUnico = "ux_clientes_email";
    private const string Colunas = "id, nome, email, criado_em";

    public async Task<Cliente?> ObterAsync(int id, CancellationToken ct)
    {
        var linha = await QuerySingleOrDefaultAsync<ClienteLinha>(
            $"SELECT {Colunas} FROM clientes WHERE id = @id", new { id }, ct);
        return linha?.ParaEntidade();
    }

    public async Task<bool> ExisteEmailAsync(string email, int? ignorarId, CancellationToken ct)
    {
        const string Sql = """
            SELECT EXISTS (
                SELECT 1 FROM clientes
                WHERE lower(email) = lower(@email) AND (@ignorarId::integer IS NULL OR id <> @ignorarId))
            """;
        var conexao = await sessao.ObterConexaoAsync(ct);
        return await conexao.ExecuteScalarAsync<bool>(new CommandDefinition(Sql, new { email, ignorarId }, sessao.Transacao, cancellationToken: ct));
    }

    public async Task<Cliente> InserirAsync(Cliente cliente, CancellationToken ct)
    {
        // O id é gerado pelo banco (identity) e devolvido no próprio INSERT.
        var id = await ExecutarTraduzindoConflitoAsync(
            (conexao, comando) => conexao.ExecuteScalarAsync<int>(comando),
            "INSERT INTO clientes (nome, email, criado_em) VALUES (@Nome, @Email, @CriadoEm) RETURNING id", cliente, ct);
        return Cliente.Restaurar(id, cliente.Nome, cliente.Email, cliente.CriadoEm);
    }

    public Task AtualizarAsync(Cliente cliente, CancellationToken ct) =>
        ExecutarTraduzindoConflitoAsync(
            (conexao, comando) => conexao.ExecuteAsync(comando),
            "UPDATE clientes SET nome = @Nome, email = @Email WHERE id = @Id", cliente, ct);

    public async Task<bool> ExcluirAsync(int id, CancellationToken ct)
    {
        var conexao = await sessao.ObterConexaoAsync(ct);
        var afetadas = await conexao.ExecuteAsync(new CommandDefinition(
            "DELETE FROM clientes WHERE id = @id", new { id }, sessao.Transacao, cancellationToken: ct));
        return afetadas > 0;
    }

    public async Task<PaginaDeClientes> ListarAsync(string? busca, int deslocamento, int quantidade, CancellationToken ct)
    {
        // count(*) OVER() devolve o total junto com a página, numa única ida ao banco.
        const string Filtro = """
            WHERE @padrao::text IS NULL
               OR nome ILIKE @padrao ESCAPE '\'
               OR email ILIKE @padrao ESCAPE '\'
            """;
        var parametros = new { padrao = PadraoDeBusca(busca), deslocamento, quantidade };

        var conexao = await sessao.ObterConexaoAsync(ct);
        var linhas = (await conexao.QueryAsync<ClienteLinha>(new CommandDefinition(
            $"""
            SELECT {Colunas}, count(*) OVER() AS total
            FROM clientes
            {Filtro}
            ORDER BY nome, id
            OFFSET @deslocamento LIMIT @quantidade
            """,
            parametros,
            sessao.Transacao,
            cancellationToken: ct))).ToList();

        // Página além do fim: não há linhas para carregar o total, então ele é contado à parte.
        var total = linhas.Count > 0
            ? linhas[0].Total
            : await conexao.ExecuteScalarAsync<long>(new CommandDefinition(
                $"SELECT count(*) FROM clientes {Filtro}", parametros, sessao.Transacao, cancellationToken: ct));

        return new PaginaDeClientes(linhas.Select(l => l.ParaEntidade()).ToList(), total);
    }

    /// <summary>Monta <c>%termo%</c> com <c>\</c>, <c>%</c> e <c>_</c> escapados; nulo quando não há busca.</summary>
    internal static string? PadraoDeBusca(string? busca)
    {
        if (string.IsNullOrWhiteSpace(busca))
        {
            return null;
        }

        var escapado = busca.Trim()
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
        return $"%{escapado}%";
    }

    private async Task<T> ExecutarTraduzindoConflitoAsync<T>(
        Func<NpgsqlConnection, CommandDefinition, Task<T>> executar, string sql, Cliente cliente, CancellationToken ct)
    {
        var conexao = await sessao.ObterConexaoAsync(ct);
        var parametros = new { cliente.Id, cliente.Nome, cliente.Email, cliente.CriadoEm };

        try
        {
            return await executar(conexao, new CommandDefinition(sql, parametros, sessao.Transacao, cancellationToken: ct));
        }
        catch (PostgresException ex) when (ex is { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: IndiceEmailUnico })
        {
            throw new ConflitoException($"Já existe um cliente com o e-mail '{cliente.Email}'.");
        }
    }

    private async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object parametros, CancellationToken ct)
    {
        var conexao = await sessao.ObterConexaoAsync(ct);
        return await conexao.QuerySingleOrDefaultAsync<T>(new CommandDefinition(sql, parametros, sessao.Transacao, cancellationToken: ct));
    }

    private sealed class ClienteLinha
    {
        public int Id { get; init; }

        public string Nome { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public DateTimeOffset CriadoEm { get; init; }

        public long Total { get; init; }

        public Cliente ParaEntidade() => Cliente.Restaurar(Id, Nome, Email, CriadoEm);
    }
}
