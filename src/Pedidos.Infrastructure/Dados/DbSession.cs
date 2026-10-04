using Npgsql;

namespace Pedidos.Infrastructure.Dados;

internal sealed class DbSession(NpgsqlDataSource dataSource) : IDbSession, IAsyncDisposable
{
    private NpgsqlConnection? _conexao;

    public NpgsqlTransaction? Transacao { get; private set; }

    public async Task<NpgsqlConnection> ObterConexaoAsync(CancellationToken ct) =>
        _conexao ??= await dataSource.OpenConnectionAsync(ct);

    internal async Task IniciarTransacaoAsync(CancellationToken ct)
    {
        if (Transacao is not null)
        {
            throw new InvalidOperationException("Já existe uma transação ativa nesta requisição.");
        }

        var conexao = await ObterConexaoAsync(ct);
        Transacao = await conexao.BeginTransactionAsync(ct);
    }

    internal async Task ConfirmarAsync(CancellationToken ct)
    {
        var transacao = Transacao ?? throw new InvalidOperationException("Nenhuma transação ativa para confirmar.");
        await transacao.CommitAsync(ct);
        await EncerrarTransacaoAsync();
    }

    internal async Task DesfazerAsync(CancellationToken ct)
    {
        if (Transacao is null)
        {
            return;
        }

        await Transacao.RollbackAsync(ct);
        await EncerrarTransacaoAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await EncerrarTransacaoAsync();

        if (_conexao is not null)
        {
            await _conexao.DisposeAsync();
            _conexao = null;
        }
    }

    private async ValueTask EncerrarTransacaoAsync()
    {
        if (Transacao is not null)
        {
            await Transacao.DisposeAsync();
            Transacao = null;
        }
    }
}
