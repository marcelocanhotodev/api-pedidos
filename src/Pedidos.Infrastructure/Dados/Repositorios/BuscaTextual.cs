namespace Pedidos.Infrastructure.Dados.Repositorios;

/// <summary>Filtro "contém" para <c>ILIKE ... ESCAPE '\'</c>, tratando curingas digitados como literais.</summary>
internal static class BuscaTextual
{
    /// <summary>Monta <c>%termo%</c> com <c>\</c>, <c>%</c> e <c>_</c> escapados; nulo quando não há busca.</summary>
    public static string? Padrao(string? busca)
    {
        if (string.IsNullOrWhiteSpace(busca))
        {
            return null;
        }

        var escapado = busca.Trim()
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
        return $"%{escapado}%";
    }
}
