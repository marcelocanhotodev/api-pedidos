using Pedidos.Application.Excecoes;
using Pedidos.Domain.Excecoes;

namespace Pedidos.Api.Configuracao;

/// <summary>Resultado HTTP de uma exceção: status, título e detalhe seguro para o cliente.</summary>
internal sealed record ErroHttp(int Status, string Titulo, string Detalhe);

/// <summary>Converte exceções em respostas HTTP sem expor detalhes internos.</summary>
internal static class MapeamentoDeExcecoes
{
    public const string DetalheGenerico = "Ocorreu um erro inesperado. Informe o traceId ao suporte.";

    public static ErroHttp Mapear(Exception excecao) => excecao switch
    {
        CredenciaisInvalidasException e => new(StatusCodes.Status401Unauthorized, "Não autorizado", e.Message),
        NaoEncontradoException e => new(StatusCodes.Status404NotFound, "Recurso não encontrado", e.Message),
        ConflitoException e => new(StatusCodes.Status409Conflict, "Conflito", e.Message),
        RegraDeNegocioException e => new(StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", e.Message),
        _ => new(StatusCodes.Status500InternalServerError, "Erro interno", DetalheGenerico),
    };
}
