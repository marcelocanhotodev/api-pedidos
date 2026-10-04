using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Pedidos.Infrastructure.Saude;

/// <summary>Considera o serviço pronto quando o PostgreSQL responde a um <c>SELECT 1</c>.</summary>
internal sealed class BancoHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var comando = dataSource.CreateCommand("SELECT 1");
            await comando.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or System.Net.Sockets.SocketException)
        {
            return HealthCheckResult.Unhealthy("Banco de dados inacessível.", ex);
        }
    }
}
