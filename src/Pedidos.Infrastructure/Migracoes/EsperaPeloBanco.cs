using Microsoft.Extensions.Logging;

namespace Pedidos.Infrastructure.Migracoes;

/// <summary>Repete uma tentativa de conexão até o banco responder ou o prazo se esgotar.</summary>
public static class EsperaPeloBanco
{
    public static readonly TimeSpan PrazoPadrao = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan IntervaloPadrao = TimeSpan.FromSeconds(1);

    /// <exception cref="InvalidOperationException">O banco não respondeu dentro do prazo.</exception>
    public static async Task AguardarAsync(
        Func<CancellationToken, Task> tentarConectar,
        ILogger logger,
        TimeProvider timeProvider,
        TimeSpan prazo,
        TimeSpan intervalo,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tentarConectar);

        var inicio = timeProvider.GetTimestamp();
        var tentativa = 0;

        while (true)
        {
            tentativa++;
            try
            {
                await tentarConectar(ct);
                if (tentativa > 1)
                {
                    logger.LogInformation("Banco disponível após {Tentativas} tentativas", tentativa);
                }

                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (timeProvider.GetElapsedTime(inicio) + intervalo > prazo)
                {
                    throw new InvalidOperationException(
                        $"Banco de dados indisponível após {prazo.TotalSeconds:0} s ({tentativa} tentativas). Verifique ConnectionStrings__Padrao e se o PostgreSQL está em execução.",
                        ex);
                }

                logger.LogWarning("Banco ainda indisponível (tentativa {Tentativa}): {Erro}", tentativa, ex.Message);
                await Task.Delay(intervalo, timeProvider, ct);
            }
        }
    }
}
