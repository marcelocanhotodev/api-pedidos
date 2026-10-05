using System.Reflection;
using DbUp;
using DbUp.Engine.Output;
using Microsoft.Extensions.Logging;

namespace Pedidos.Infrastructure.Migracoes;

/// <summary>
/// Aplica, em ordem, os scripts <c>.sql</c> embutidos que ainda não constam em <c>schemaversions</c>.
/// Cada script roda em sua própria transação.
/// </summary>
public sealed class MigradorDeBanco(ILogger<MigradorDeBanco> logger)
{
    private static readonly Assembly AssemblyPadrao = typeof(MigradorDeBanco).Assembly;

    /// <returns>Nomes dos scripts executados nesta chamada.</returns>
    /// <exception cref="InvalidOperationException">Um script falhou (a transação dele foi desfeita).</exception>
    public IReadOnlyList<string> Executar(string connectionString, Assembly? assemblyDosScripts = null)
    {
        var assembly = assemblyDosScripts ?? AssemblyPadrao;

        // Migrações rodam uma vez no startup: sem pool, a conexão é fechada de fato ao terminar
        // em vez de ficar ociosa no pool global do Npgsql ocupando uma conexão do PostgreSQL.
        var semPool = new Npgsql.NpgsqlConnectionStringBuilder(connectionString) { Pooling = false }.ConnectionString;

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(semPool)
            .WithScriptsEmbeddedInAssembly(assembly, nome => nome.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .WithTransactionPerScript()
            .LogTo(new LogDoDbUp(logger))
            .Build();

        var resultado = upgrader.PerformUpgrade();

        if (!resultado.Successful)
        {
            logger.LogError(resultado.Error, "Falha ao aplicar o script de migração {Script}", resultado.ErrorScript?.Name);
            throw new InvalidOperationException(
                $"Falha ao aplicar o script de migração '{resultado.ErrorScript?.Name}'.", resultado.Error);
        }

        var executados = resultado.Scripts.Select(s => s.Name).ToList();
        logger.LogInformation("Migrações aplicadas: {Quantidade}", executados.Count);
        return executados;
    }

    private sealed class LogDoDbUp(ILogger logger) : IUpgradeLog
    {
        public void LogTrace(string format, params object[] args) => logger.LogTrace("{Mensagem}", Formatar(format, args));

        public void LogDebug(string format, params object[] args) => logger.LogDebug("{Mensagem}", Formatar(format, args));

        public void LogInformation(string format, params object[] args) => logger.LogInformation("{Mensagem}", Formatar(format, args));

        public void LogWarning(string format, params object[] args) => logger.LogWarning("{Mensagem}", Formatar(format, args));

        public void LogError(string format, params object[] args) => logger.LogError("{Mensagem}", Formatar(format, args));

        public void LogError(Exception ex, string format, params object[] args) => logger.LogError(ex, "{Mensagem}", Formatar(format, args));

        private static string Formatar(string format, object[] args) =>
            args.Length == 0 ? format : string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args);
    }
}
