using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Pedidos.Api.Configuracao;
using Pedidos.Application.Excecoes;
using Pedidos.Domain.Excecoes;

namespace Pedidos.UnitTests.Api;

/// <summary>Requisito: Formato Padrão de Erros.</summary>
public class MapeamentoDeExcecoesTests
{
    [Fact]
    public void Mapear_CredenciaisInvalidas_Responde401()
    {
        var erro = MapeamentoDeExcecoes.Mapear(new CredenciaisInvalidasException());

        Assert.Equal(StatusCodes.Status401Unauthorized, erro.Status);
        Assert.Equal("Usuário ou senha inválidos.", erro.Detalhe);
    }

    [Fact]
    public void Mapear_NaoEncontrado_Responde404ComMensagemDoDominio()
    {
        var erro = MapeamentoDeExcecoes.Mapear(new NaoEncontradoException("Cliente", 7));

        Assert.Equal(StatusCodes.Status404NotFound, erro.Status);
        Assert.Equal("Cliente '7' não foi encontrado.", erro.Detalhe);
    }

    [Fact]
    public void Mapear_Conflito_Responde409()
    {
        var erro = MapeamentoDeExcecoes.Mapear(new ConflitoException("E-mail já cadastrado."));

        Assert.Equal(StatusCodes.Status409Conflict, erro.Status);
        Assert.Equal("E-mail já cadastrado.", erro.Detalhe);
    }

    [Fact]
    public void Mapear_RegraDeNegocio_Responde422()
    {
        var erro = MapeamentoDeExcecoes.Mapear(new RegraDeNegocioException("Estoque insuficiente."));

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, erro.Status);
        Assert.Equal("Estoque insuficiente.", erro.Detalhe);
    }

    [Fact]
    public void Mapear_ExcecaoInesperada_Responde500SemExporMensagemInterna()
    {
        var erro = MapeamentoDeExcecoes.Mapear(new InvalidOperationException("senha=segredo; at Npgsql..."));

        Assert.Equal(StatusCodes.Status500InternalServerError, erro.Status);
        Assert.Equal(MapeamentoDeExcecoes.DetalheGenerico, erro.Detalhe);
        Assert.DoesNotContain("segredo", erro.Detalhe, StringComparison.Ordinal);
    }

    [Fact]
    public void CriarProblemaDeValidacao_FalhasEmCampos_AgrupaMensagensPorCampo()
    {
        var falhas = new[]
        {
            new ValidationFailure("email", "E-mail inválido."),
            new ValidationFailure("email", "E-mail muito longo."),
            new ValidationFailure("nome", "Nome obrigatório."),
        };

        var problema = ProblemDetailsConfig.CriarProblemaDeValidacao(falhas, new DefaultHttpContext(), StatusCodes.Status400BadRequest);

        Assert.Equal(400, problema.Status);
        Assert.Equal(["E-mail inválido.", "E-mail muito longo."], problema.Errors["email"]);
        Assert.Equal(["Nome obrigatório."], problema.Errors["nome"]);
        Assert.True(problema.Extensions.ContainsKey("traceId"));
    }
}
