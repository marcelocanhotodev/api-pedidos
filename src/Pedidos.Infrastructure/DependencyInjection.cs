using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Pedidos.Application.Abstracoes;
using Pedidos.Domain.Repositorios;
using Pedidos.Infrastructure.Dados;
using Pedidos.Infrastructure.Dados.Repositorios;
using Pedidos.Infrastructure.Migracoes;
using Pedidos.Infrastructure.Saude;
using Pedidos.Infrastructure.Seguranca;

namespace Pedidos.Infrastructure;

public static class DependencyInjection
{
    public const string NomeConnectionString = "Padrao";
    public const string ChaveAplicarMigracoes = "APLICAR_MIGRACOES";
    public const string TagProntidao = "ready";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(NomeConnectionString);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"A variável de ambiente 'ConnectionStrings__{NomeConnectionString}' não foi definida.");
        }

        // Mapeia colunas snake_case (criado_em) para propriedades PascalCase (CriadoEm) em todas as consultas.
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        // Datas timestamptz lidas e gravadas como DateTimeOffset em UTC.
        SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
        SqlMapper.AddTypeHandler(new DateTimeOffsetUtcHandler());

        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRelogio, RelogioDoSistema>();
        services.AddSingleton<MigradorDeBanco>();

        services.AddScoped<DbSession>();
        services.AddScoped<IDbSession>(sp => sp.GetRequiredService<DbSession>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IClienteRepository, ClienteRepository>();

        services.AddHealthChecks()
            .AddCheck<BancoHealthCheck>("banco", tags: [TagProntidao]);

        return services;
    }

    /// <summary>
    /// Registra emissão de token e validação de credenciais com as opções já validadas.
    /// </summary>
    public static IServiceCollection AddSeguranca(this IServiceCollection services, OpcoesDeSeguranca opcoes)
    {
        services.AddSingleton(opcoes);
        services.AddSingleton<IEmissorDeToken, EmissorDeTokenJwt>();
        services.AddSingleton<IValidadorDeCredenciais, ValidadorDeCredenciais>();
        return services;
    }

    /// <summary>
    /// Quando <c>APLICAR_MIGRACOES=true</c>, aguarda o banco (até 30 s) e aplica as migrações pendentes.
    /// </summary>
    public static async Task AplicarMigracoesAsync(this IServiceProvider services, IConfiguration configuration, CancellationToken ct)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(MigradorDeBanco).FullName!);

        if (!bool.TryParse(configuration[ChaveAplicarMigracoes], out var aplicar) || !aplicar)
        {
            logger.LogInformation("{Chave} desabilitado; esquema do banco não será alterado", ChaveAplicarMigracoes);
            return;
        }

        var dataSource = services.GetRequiredService<NpgsqlDataSource>();
        await EsperaPeloBanco.AguardarAsync(
            async token =>
            {
                await using var conexao = await dataSource.OpenConnectionAsync(token);
            },
            logger,
            services.GetRequiredService<TimeProvider>(),
            EsperaPeloBanco.PrazoPadrao,
            EsperaPeloBanco.IntervaloPadrao,
            ct);

        // NpgsqlDataSource.ConnectionString omite a senha; o DbUp precisa da string original.
        services.GetRequiredService<MigradorDeBanco>().Executar(configuration.GetConnectionString(NomeConnectionString)!);
    }
}
