using Microsoft.Extensions.Logging.Abstractions;
using Pedidos.Infrastructure.Migracoes;

namespace Pedidos.UnitTests.Infrastructure;

/// <summary>Requisito: Espera pelo Banco.</summary>
public class EsperaPeloBancoTests
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMilliseconds(10);

    [Fact]
    public async Task AguardarAsync_BancoAindaSubindo_RepeteAteConectar()
    {
        var tentativas = 0;

        await EsperaPeloBanco.AguardarAsync(
            _ => ++tentativas < 3 ? throw new TimeoutException("ainda subindo") : Task.CompletedTask,
            NullLogger.Instance,
            TimeProvider.System,
            prazo: TimeSpan.FromSeconds(5),
            Intervalo,
            CancellationToken.None);

        Assert.Equal(3, tentativas);
    }

    [Fact]
    public async Task AguardarAsync_BancoDisponivel_ConectaNaPrimeiraTentativa()
    {
        var tentativas = 0;

        await EsperaPeloBanco.AguardarAsync(
            _ =>
            {
                tentativas++;
                return Task.CompletedTask;
            },
            NullLogger.Instance,
            TimeProvider.System,
            prazo: TimeSpan.FromSeconds(5),
            Intervalo,
            CancellationToken.None);

        Assert.Equal(1, tentativas);
    }

    [Fact]
    public async Task AguardarAsync_BancoIndisponivelAlemDoPrazo_FalhaComMensagemClara()
    {
        var excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => EsperaPeloBanco.AguardarAsync(
            _ => throw new TimeoutException("recusado"),
            NullLogger.Instance,
            TimeProvider.System,
            prazo: TimeSpan.FromMilliseconds(100),
            Intervalo,
            CancellationToken.None));

        Assert.StartsWith("Banco de dados indisponível", excecao.Message, StringComparison.Ordinal);
        Assert.IsType<TimeoutException>(excecao.InnerException);
    }

    [Fact]
    public void PrazoPadrao_ConformeSpec_Eh30Segundos()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), EsperaPeloBanco.PrazoPadrao);
    }
}
