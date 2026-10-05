using Pedidos.Infrastructure;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Pedidos.Api.Configuracao;

/// <summary>
/// <c>dotnet Pedidos.Api.dll --migrar</c>: aguarda o banco, aplica as migrações pendentes e encerra, sem servidor
/// HTTP e sem exigir a configuração de segurança. Usado pelo pipeline de entrega antes do deploy.
/// </summary>
internal static class ModoMigracao
{
    public const string Argumento = "--migrar";

    public static bool FoiSolicitado(string[] args) => args.Contains(Argumento, StringComparer.Ordinal);

    /// <returns>Código de saída do processo: 0 em sucesso, 1 em falha.</returns>
    public static async Task<int> ExecutarAsync(string[] args, Serilog.ILogger logDeFalha, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(logDeFalha);

        try
        {
            // O argumento do modo não é configuração: fora dele, "--chave=valor" continua valendo (ex.: nos testes).
            var builder = Host.CreateApplicationBuilder(args.Where(a => a != Argumento).ToArray());
            builder.Services.AddSerilog(
                configuracao => configuracao
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .WriteTo.Console(new JsonFormatter(renderMessage: true)),
                preserveStaticLogger: true);
            builder.Services.AddInfrastructure(builder.Configuration);

            using var host = builder.Build();
            var executados = await host.Services.MigrarAsync(builder.Configuration, ct);

            host.Services.GetRequiredService<ILogger<Program>>()
                .LogInformation("Modo {Modo} concluído: {Quantidade} script(s) aplicado(s)", Argumento, executados.Count);
            return 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logDeFalha.Fatal(ex, "Falha no modo {Modo}", Argumento);
            return 1;
        }
    }
}
