using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Pedidos.Api.Configuracao;

/// <summary>Único ponto que transforma exceções em <c>application/problem+json</c>.</summary>
internal sealed class TratadorGlobalDeExcecoes(
    IProblemDetailsService problemDetails,
    ILogger<TratadorGlobalDeExcecoes> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var erro = MapeamentoDeExcecoes.Mapear(exception);

        if (erro.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Erro não tratado em {Metodo} {Caminho}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = erro.Status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = erro.Status,
                Title = erro.Titulo,
                Detail = erro.Detalhe,
            },
        });
    }
}
