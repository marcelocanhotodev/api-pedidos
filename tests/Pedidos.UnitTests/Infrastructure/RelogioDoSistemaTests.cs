using Pedidos.Infrastructure.Dados;

namespace Pedidos.UnitTests.Infrastructure;

/// <summary>Datas gravadas e devolvidas com a mesma precisão do PostgreSQL (microssegundos).</summary>
public class RelogioDoSistemaTests
{
    [Fact]
    public void AgoraUtc_HorarioComTicksAbaixoDeMicrossegundo_TruncaEmMicrossegundos()
    {
        var horario = new DateTimeOffset(2026, 10, 5, 14, 4, 38, TimeSpan.Zero).AddTicks(966_619); // ,0966619 s
        var relogio = new RelogioDoSistema(new TimeProviderFixo(horario));

        Assert.Equal(new DateTimeOffset(2026, 10, 5, 14, 4, 38, TimeSpan.Zero).AddTicks(966_610), relogio.AgoraUtc);
    }

    private sealed class TimeProviderFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }
}
