namespace Pedidos.Application.Abstracoes;

/// <summary>
/// Controla a transação compartilhada pelos repositórios da requisição atual.
/// </summary>
public interface IUnitOfWork
{
    Task IniciarAsync(CancellationToken ct);

    Task CommitAsync(CancellationToken ct);

    Task RollbackAsync(CancellationToken ct);
}
