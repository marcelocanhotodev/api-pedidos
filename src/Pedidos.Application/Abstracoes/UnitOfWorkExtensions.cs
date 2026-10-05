namespace Pedidos.Application.Abstracoes;

public static class UnitOfWorkExtensions
{
    /// <summary>
    /// Executa <paramref name="operacao"/> numa transação: confirma em caso de sucesso e desfaz em qualquer falha.
    /// </summary>
    public static async Task<T> EmTransacaoAsync<T>(this IUnitOfWork unitOfWork, Func<Task<T>> operacao, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(operacao);

        await unitOfWork.IniciarAsync(ct);
        try
        {
            var resultado = await operacao();
            await unitOfWork.CommitAsync(ct);
            return resultado;
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
