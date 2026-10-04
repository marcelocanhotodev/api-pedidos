using System.Diagnostics;
using FastEndpoints;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Pedidos.Api.Configuracao;

internal static class ProblemDetailsConfig
{
    public const string ContentType = "application/problem+json";

    public static IServiceCollection AddProblemDetailsPadrao(this IServiceCollection services)
    {
        services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
        {
            ctx.ProblemDetails.Extensions["traceId"] = ObterTraceId(ctx.HttpContext);
            // Nunca expor detalhes de exceção (stack trace) ao cliente.
            ctx.ProblemDetails.Extensions.Remove("exception");
        });
        services.AddExceptionHandler<TratadorGlobalDeExcecoes>();
        return services;
    }

    /// <summary>
    /// Falhas de validação do FastEndpoints no formato RFC 7807, com <c>errors</c> mapeando campo → mensagens.
    /// </summary>
    public static void ConfigurarErrosDeValidacao(ErrorOptions erros)
    {
        erros.ContentType = ContentType;
        erros.ResponseBuilder = (falhas, ctx, status) => CriarProblemaDeValidacao(falhas, ctx, status);
    }

    internal static ValidationProblemDetails CriarProblemaDeValidacao(
        IEnumerable<ValidationFailure> falhas, HttpContext ctx, int status)
    {
        var errosPorCampo = falhas
            .GroupBy(f => f.PropertyName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct().ToArray(), StringComparer.Ordinal);

        var problema = new ValidationProblemDetails(errosPorCampo)
        {
            Status = status,
            Title = "Requisição inválida",
            Detail = "Um ou mais campos não passaram na validação.",
            Instance = ctx.Request.Path,
        };
        problema.Extensions["traceId"] = ObterTraceId(ctx);
        return problema;
    }

    private static string ObterTraceId(HttpContext ctx) => Activity.Current?.TraceId.ToString() ?? ctx.TraceIdentifier;
}
