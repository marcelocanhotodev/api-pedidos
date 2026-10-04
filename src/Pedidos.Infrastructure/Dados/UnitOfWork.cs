using Pedidos.Application.Abstracoes;

namespace Pedidos.Infrastructure.Dados;

internal sealed class UnitOfWork(DbSession sessao) : IUnitOfWork
{
    public Task IniciarAsync(CancellationToken ct) => sessao.IniciarTransacaoAsync(ct);

    public Task CommitAsync(CancellationToken ct) => sessao.ConfirmarAsync(ct);

    public Task RollbackAsync(CancellationToken ct) => sessao.DesfazerAsync(ct);
}
