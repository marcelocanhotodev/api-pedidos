using Pedidos.Application.Abstracoes;

namespace Pedidos.Infrastructure.Dados;

internal sealed class RelogioDoSistema(TimeProvider timeProvider) : IRelogio
{
    public DateTimeOffset AgoraUtc => timeProvider.GetUtcNow();
}
