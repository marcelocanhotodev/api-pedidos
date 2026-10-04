using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Pedidos.Api.Configuracao;
using Pedidos.Application;
using Pedidos.Application.Abstracoes;
using Pedidos.Infrastructure;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new JsonFormatter(renderMessage: true))
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((_, configuracao) => configuracao
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(new JsonFormatter(renderMessage: true)));

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddAutenticacaoJwt(builder.Configuration)
        .AddProblemDetailsPadrao()
        .AddSingleton<IInformacoesDaAplicacao, InformacoesDaAplicacao>()
        .AddFastEndpoints()
        .SwaggerDocument(o => o.DocumentSettings = s =>
        {
            s.Title = "API de Pedidos";
            s.Version = "v1";
        });

    var app = builder.Build();

    await app.Services.AplicarMigracoesAsync(app.Configuration, app.Lifetime.ApplicationStopping);

    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseFastEndpoints(c => ProblemDetailsConfig.ConfigurarErrosDeValidacao(c.Errors));
    app.UseSwaggerGen();

    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = registro => registro.Tags.Contains(Pedidos.Infrastructure.DependencyInjection.TagProntidao),
    });

    await app.RunAsync();
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "A aplicação encerrou por falha na inicialização");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

#pragma warning disable CA1515 // Program precisa ser público para o WebApplicationFactory dos testes de integração.
public partial class Program;
#pragma warning restore CA1515
