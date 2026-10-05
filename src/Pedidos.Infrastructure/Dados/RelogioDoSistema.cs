using Pedidos.Application.Abstracoes;

namespace Pedidos.Infrastructure.Dados;

/// <summary>
/// Hora atual em UTC truncada em microssegundos, a precisão do <c>timestamptz</c> do PostgreSQL:
/// o valor devolvido na criação de um registro é idêntico ao lido depois do banco.
/// </summary>
internal sealed class RelogioDoSistema(TimeProvider timeProvider) : IRelogio
{
    private const long TicksPorMicrossegundo = TimeSpan.TicksPerMillisecond / 1000;

    public DateTimeOffset AgoraUtc
    {
        get
        {
            var agora = timeProvider.GetUtcNow();
            return agora.AddTicks(-(agora.Ticks % TicksPorMicrossegundo));
        }
    }
}
