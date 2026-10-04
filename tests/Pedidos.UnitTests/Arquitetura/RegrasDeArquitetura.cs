using System.Reflection;
using FastEndpoints;
using Pedidos.Application.Abstracoes;

namespace Pedidos.UnitTests.Arquitetura;

/// <summary>
/// Regras de arquitetura parametrizadas por tipos/assemblies, para que possam ser aplicadas
/// tanto à solução real quanto a fixtures com violações propositais.
/// Cada regra devolve a lista de violações encontradas (vazia = conforme).
/// </summary>
public static class RegrasDeArquitetura
{
    private static readonly string[] PacotesDeInfraestrutura = ["Dapper", "Npgsql", "FastEndpoints"];

    private static readonly string[] NamespacesProibidosEmCasosDeUso =
        ["Dapper", "Npgsql", "FastEndpoints", "Microsoft.AspNetCore"];

    private static readonly string[] TiposDeAcessoADadosProibidosEmEndpoints =
        ["IUnitOfWork", "IDbSession", "NpgsqlConnection", "NpgsqlDataSource", "DbConnection", "IDbConnection"];

    /// <summary>O assembly não pode referenciar nenhum dos assemblies proibidos (por nome ou prefixo).</summary>
    public static IReadOnlyList<string> ReferenciasProibidas(Assembly assembly, params string[] proibidos)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.GetReferencedAssemblies()
            .Select(r => r.Name!)
            .Where(nome => proibidos.Any(p => nome.Equals(p, StringComparison.Ordinal) || nome.StartsWith(p + ".", StringComparison.Ordinal)))
            .Select(nome => $"{assembly.GetName().Name} referencia {nome}")
            .ToList();
    }

    public static IReadOnlyList<string> ReferenciasAPacotesDeInfraestrutura(Assembly assembly) =>
        ReferenciasProibidas(assembly, PacotesDeInfraestrutura);

    public static IReadOnlyList<string> ReferenciasAEntityFramework(Assembly assembly) =>
        ReferenciasProibidas(assembly, "Microsoft.EntityFrameworkCore", "EntityFramework");

    /// <summary>Todas as classes concretas que implementam <see cref="IUseCase{TEntrada,TSaida}"/>.</summary>
    public static IReadOnlyList<Type> CasosDeUso(Assembly assembly) =>
        TiposConcretos(assembly).Where(t => InterfaceDeCasoDeUso(t) is not null).ToList();

    /// <summary>Todos os endpoints FastEndpoints concretos do assembly.</summary>
    public static IReadOnlyList<Type> Endpoints(Assembly assembly) =>
        TiposConcretos(assembly).Where(t => typeof(IEndpoint).IsAssignableFrom(t)).ToList();

    /// <summary>
    /// Convenção: <c>&lt;Nome&gt;UseCase</c> em <c>&lt;raiz&gt;.&lt;Area&gt;.&lt;Nome&gt;</c>, com <c>&lt;Nome&gt;Entrada</c> e
    /// <c>&lt;Nome&gt;Saida</c> (ou <see cref="Vazio"/>) no mesmo namespace.
    /// </summary>
    public static IReadOnlyList<string> ViolacoesDeConvencao(IEnumerable<Type> casosDeUso, string namespaceRaiz)
    {
        var violacoes = new List<string>();

        foreach (var tipo in casosDeUso)
        {
            if (!tipo.Name.EndsWith("UseCase", StringComparison.Ordinal))
            {
                violacoes.Add($"{tipo.FullName}: nome deve terminar em 'UseCase'");
                continue;
            }

            var nome = tipo.Name[..^"UseCase".Length];
            var namespaceEsperadoPrefixo = namespaceRaiz + ".";
            var dentroDaRaiz = (tipo.Namespace ?? string.Empty).StartsWith(namespaceEsperadoPrefixo, StringComparison.Ordinal);
            var segmentosAbaixoDaRaiz = dentroDaRaiz
                ? tipo.Namespace![namespaceEsperadoPrefixo.Length..].Split('.')
                : [];

            if (segmentosAbaixoDaRaiz.Length != 2 || segmentosAbaixoDaRaiz[1] != nome)
            {
                violacoes.Add($"{tipo.FullName}: deve estar em '{namespaceRaiz}.<Area>.{nome}'");
                continue;
            }

            var (entrada, saida) = ArgumentosDoCasoDeUso(tipo);
            if (entrada.Name != nome + "Entrada" || entrada.Namespace != tipo.Namespace)
            {
                violacoes.Add($"{tipo.FullName}: entrada deve ser '{nome}Entrada' no mesmo namespace (encontrado {entrada.Name})");
            }

            if (saida != typeof(Vazio) && (saida.Name != nome + "Saida" || saida.Namespace != tipo.Namespace))
            {
                violacoes.Add($"{tipo.FullName}: saída deve ser '{nome}Saida' no mesmo namespace ou Vazio (encontrado {saida.Name})");
            }
        }

        return violacoes;
    }

    /// <summary>
    /// Casos de uso não podem depender de infraestrutura (Dapper, Npgsql, FastEndpoints, ASP.NET Core/HttpContext)
    /// nem de outro caso de uso.
    /// </summary>
    public static IReadOnlyList<string> ViolacoesDeDependenciaDosCasosDeUso(IEnumerable<Type> casosDeUso)
    {
        var violacoes = new List<string>();

        foreach (var tipo in casosDeUso)
        {
            foreach (var dependencia in TiposUsadosPor(tipo))
            {
                if (NamespacesProibidosEmCasosDeUso.Any(ns => EstaNoNamespace(dependencia, ns)))
                {
                    violacoes.Add($"{tipo.Name} depende de {dependencia.FullName}");
                }
            }

            foreach (var parametro in ParametrosDosConstrutores(tipo))
            {
                if (EhInterfaceDeCasoDeUso(parametro.ParameterType) || InterfaceDeCasoDeUso(parametro.ParameterType) is not null)
                {
                    violacoes.Add($"{tipo.Name} depende de outro caso de uso ({parametro.ParameterType.Name})");
                }
            }
        }

        return violacoes;
    }

    /// <summary>
    /// Endpoints recebem exatamente um <see cref="IUseCase{TEntrada,TSaida}"/> e nunca repositório,
    /// <see cref="IUnitOfWork"/> ou conexão; cada caso de uso é usado por exatamente um endpoint.
    /// </summary>
    public static IReadOnlyList<string> ViolacoesDosEndpoints(IEnumerable<Type> endpoints, IEnumerable<Type> casosDeUso)
    {
        var violacoes = new List<string>();
        var usos = new Dictionary<Type, List<Type>>();

        foreach (var endpoint in endpoints)
        {
            var parametros = ParametrosDosConstrutores(endpoint).Select(p => p.ParameterType).ToList();

            foreach (var parametro in parametros)
            {
                if (parametro.Name.EndsWith("Repository", StringComparison.Ordinal)
                    || TiposDeAcessoADadosProibidosEmEndpoints.Contains(parametro.Name))
                {
                    violacoes.Add($"{endpoint.Name} recebe {parametro.Name}; endpoints devem receber apenas IUseCase<,>");
                }
            }

            var casosDoEndpoint = parametros.Where(EhInterfaceDeCasoDeUso).ToList();
            if (casosDoEndpoint.Count != 1)
            {
                violacoes.Add($"{endpoint.Name} depende de {casosDoEndpoint.Count} casos de uso; esperado exatamente 1");
            }

            foreach (var caso in casosDoEndpoint)
            {
                if (!usos.TryGetValue(caso, out var lista))
                {
                    usos[caso] = lista = [];
                }

                lista.Add(endpoint);
            }
        }

        foreach (var casoDeUso in casosDeUso)
        {
            var interfaceDoCaso = InterfaceDeCasoDeUso(casoDeUso)!;
            var quantidade = usos.TryGetValue(interfaceDoCaso, out var lista) ? lista.Count : 0;
            if (quantidade != 1)
            {
                violacoes.Add($"{casoDeUso.Name} é usado por {quantidade} endpoints; esperado exatamente 1");
            }
        }

        return violacoes;
    }

    private static IEnumerable<Type> TiposConcretos(Assembly assembly) =>
        assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });

    private static Type? InterfaceDeCasoDeUso(Type tipo) =>
        tipo.GetInterfaces().FirstOrDefault(EhInterfaceDeCasoDeUso);

    private static bool EhInterfaceDeCasoDeUso(Type tipo) =>
        tipo.IsGenericType && tipo.GetGenericTypeDefinition() == typeof(IUseCase<,>);

    private static (Type Entrada, Type Saida) ArgumentosDoCasoDeUso(Type tipo)
    {
        var argumentos = InterfaceDeCasoDeUso(tipo)!.GetGenericArguments();
        return (argumentos[0], argumentos[1]);
    }

    private static IEnumerable<ParameterInfo> ParametrosDosConstrutores(Type tipo) =>
        tipo.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .SelectMany(c => c.GetParameters());

    private static IEnumerable<Type> TiposUsadosPor(Type tipo)
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        var tipos = ParametrosDosConstrutores(tipo).Select(p => p.ParameterType)
            .Concat(tipo.GetFields(Flags).Select(f => f.FieldType))
            .Concat(tipo.GetProperties(Flags).Select(p => p.PropertyType))
            .Concat(tipo.GetMethods(Flags).SelectMany(m => m.GetParameters().Select(p => p.ParameterType).Append(m.ReturnType)));

        return tipos.SelectMany(ExpandirGenericos).Distinct();
    }

    private static IEnumerable<Type> ExpandirGenericos(Type tipo)
    {
        yield return tipo;

        if (tipo.HasElementType)
        {
            foreach (var t in ExpandirGenericos(tipo.GetElementType()!))
            {
                yield return t;
            }
        }

        if (tipo.IsGenericType)
        {
            foreach (var argumento in tipo.GetGenericArguments().SelectMany(ExpandirGenericos))
            {
                yield return argumento;
            }
        }
    }

    private static bool EstaNoNamespace(Type tipo, string ns) =>
        tipo.Namespace is { } n && (n == ns || n.StartsWith(ns + ".", StringComparison.Ordinal));
}
