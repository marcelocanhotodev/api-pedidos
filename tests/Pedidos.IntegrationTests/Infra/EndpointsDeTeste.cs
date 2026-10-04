using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Pedidos.Domain.Excecoes;

namespace Pedidos.IntegrationTests.Infra;

// Endpoints que existem apenas no assembly de testes (descobertos pelo FastEndpoints quando a suíte roda),
// usados para exercitar o tratamento global de erros antes de existirem endpoints de negócio.
#pragma warning disable CA1812 // Instanciados pelo FastEndpoints via reflexão.

internal sealed class ErroDeTesteEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/teste/erros/{tipo}");
        AllowAnonymous();
        Description(d => d.ExcludeFromDescription());
    }

    public override Task HandleAsync(CancellationToken ct) => Route<string>("tipo") switch
    {
        "nao-encontrado" => throw new NaoEncontradoException("Recurso", 42),
        "conflito" => throw new ConflitoException("Valor já cadastrado."),
        "regra" => throw new RegraDeNegocioException("Estoque insuficiente."),
        _ => throw new InvalidOperationException("Falha interna com detalhe sigiloso: senha=123"),
    };
}

/// <summary>Endpoint protegido sob /api/v1 (sem AllowAnonymous), usado para provar a exigência de token.</summary>
internal sealed class ProtegidoDeTesteEndpoint : EndpointWithoutRequest<string>
{
    public override void Configure()
    {
        Get("teste-protegido");
        Group<Pedidos.Api.Endpoints.ApiV1>();
        Summary(s => s.Summary = "Endpoint protegido de teste");
    }

    public override Task HandleAsync(CancellationToken ct) => Send.OkAsync(User.Identity?.Name ?? string.Empty, ct);
}

internal sealed class ValidacaoDeTesteRequest
{
    public string Nome { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}

internal sealed class ValidacaoDeTesteValidator : Validator<ValidacaoDeTesteRequest>
{
    public ValidacaoDeTesteValidator()
    {
        RuleFor(r => r.Nome).NotEmpty().WithMessage("nome é obrigatório.");
        RuleFor(r => r.Email).EmailAddress().WithMessage("email inválido.");
    }
}

internal sealed class ValidacaoDeTesteEndpoint : Endpoint<ValidacaoDeTesteRequest>
{
    public override void Configure()
    {
        Post("/teste/validacao");
        AllowAnonymous();
        Description(d => d.ExcludeFromDescription());
    }

    public override Task HandleAsync(ValidacaoDeTesteRequest req, CancellationToken ct) => Send.NoContentAsync(ct);
}
