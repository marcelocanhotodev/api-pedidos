using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace Pedidos.IntegrationTests.Infra;

/// <summary>Rota registrada na aplicação: caminho, tipo do endpoint e se aceita acesso anônimo.</summary>
public sealed record RotaRegistrada(string Caminho, Type TipoDoEndpoint, bool Anonima);

/// <summary>
/// Regras verificadas sobre as rotas efetivamente registradas (AllowAnonymous e Group são definidos em
/// <c>Configure()</c>, em tempo de execução, e não aparecem nos tipos).
/// </summary>
public static class RegrasDeRotas
{
    public const string PrefixoApi = "/api/v1/";
    public const string RotaDoToken = "/auth/token";
    public const string NamespaceDosEndpoints = "Pedidos.Api.Endpoints";

    /// <summary>Endpoints públicos que ficam fora de /api/v1 de propósito: autenticação e informações operacionais.</summary>
    public static readonly IReadOnlyList<string> NamespacesForaDoGrupo =
        ["Pedidos.Api.Endpoints.Auth", "Pedidos.Api.Endpoints.Sistema"];

    public static IReadOnlyList<RotaRegistrada> Ler(EndpointDataSource fonte)
    {
        ArgumentNullException.ThrowIfNull(fonte);

        return fonte.Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => (Endpoint: e, Definicao: e.Metadata.GetMetadata<EndpointDefinition>()))
            .Where(x => x.Definicao is not null)
            .Select(x => new RotaRegistrada(
                "/" + x.Endpoint.RoutePattern.RawText!.TrimStart('/'),
                x.Definicao!.EndpointType,
                x.Endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null))
            .ToList();
    }

    /// <summary>Nenhuma rota de /api/v1 pode aceitar acesso anônimo, sem exceção.</summary>
    public static IReadOnlyList<string> AnonimasIndevidas(IEnumerable<RotaRegistrada> rotas) =>
        rotas
            .Where(r => r.Anonima && r.Caminho.StartsWith(PrefixoApi, StringComparison.OrdinalIgnoreCase))
            .Select(r => $"{r.Caminho} ({r.TipoDoEndpoint.Name}) aceita acesso anônimo")
            .ToList();

    /// <summary>
    /// Todo endpoint de negócio (Pedidos.Api.Endpoints fora de <see cref="NamespacesForaDoGrupo"/>) fica sob /api/v1 (Group&lt;ApiV1&gt;).
    /// </summary>
    public static IReadOnlyList<string> ForaDoGrupoApiV1(IEnumerable<RotaRegistrada> rotas) =>
        rotas
            .Where(r => EstaNoNamespace(r.TipoDoEndpoint, NamespaceDosEndpoints))
            .Where(r => !NamespacesForaDoGrupo.Any(ns => EstaNoNamespace(r.TipoDoEndpoint, ns)))
            .Where(r => !r.Caminho.StartsWith(PrefixoApi, StringComparison.OrdinalIgnoreCase))
            .Select(r => $"{r.TipoDoEndpoint.Name} está em {r.Caminho}, fora de /api/v1 (falta Group<ApiV1>)")
            .ToList();

    private static bool EstaNoNamespace(Type tipo, string ns) =>
        tipo.Namespace is { } n && (n == ns || n.StartsWith(ns + ".", StringComparison.Ordinal));
}
