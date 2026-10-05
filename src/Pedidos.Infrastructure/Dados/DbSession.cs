using Npgsql;

namespace Pedidos.Infrastructure.Dados;

/// <remarks>
/// Implementa <see cref="IDisposable"/> além de <see cref="IAsyncDisposable"/>: alguns escopos de DI são descartados
/// de forma síncrona (ex.: o FastEndpoints resolve os endpoints num escopo síncrono ao iniciar), e o contêiner
/// recusa descartar sincronamente um serviço que só oferece <see cref="IAsyncDisposable"/>.
/// </remarks>
internal sealed class DbSession(NpgsqlDataSource dataSource) : IDbSession, IAsyncDisposable, IDisposable
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

    public void Dispose()
    {
        Transacao?.Dispose();
        Transacao = null;
        _conexao?.Dispose();
        _conexao = null;
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
