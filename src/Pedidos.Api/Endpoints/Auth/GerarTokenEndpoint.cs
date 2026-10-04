using FastEndpoints;
using FluentValidation;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Auth.GerarToken;

namespace Pedidos.Api.Endpoints.Auth;

public sealed class GerarTokenRequest
{
    public string Usuario { get; init; } = string.Empty;

    public string Senha { get; init; } = string.Empty;
}

/// <param name="AccessToken">JWT para o cabeçalho <c>Authorization: Bearer</c>.</param>
/// <param name="ExpiraEm">Validade do token em segundos a partir da emissão.</param>
public sealed record GerarTokenResponse(string AccessToken, int ExpiraEm);

public sealed class GerarTokenValidator : Validator<GerarTokenRequest>
{
    public GerarTokenValidator()
    {
        RuleFor(r => r.Usuario).NotEmpty().WithMessage("usuario é obrigatório.");
        RuleFor(r => r.Senha).NotEmpty().WithMessage("senha é obrigatória.");
    }
}

internal sealed class GerarTokenEndpoint(IUseCase<GerarTokenEntrada, GerarTokenSaida> casoDeUso)
    : Endpoint<GerarTokenRequest, GerarTokenResponse>
{
    public override void Configure()
    {
        Post("auth/token");
        Group<ApiV1>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Emitir token de acesso";
            s.Description = "Troca usuário e senha configurados por um JWT a ser enviado como `Authorization: Bearer <token>`.";
            s.ExampleRequest = new GerarTokenRequest { Usuario = "admin", Senha = "sua-senha" };
            s.Responses[StatusCodes.Status200OK] = "Token emitido.";
            s.Responses[StatusCodes.Status400BadRequest] = "Usuário ou senha não informados.";
            s.Responses[StatusCodes.Status401Unauthorized] = "Credenciais inválidas.";
        });
    }

    public override async Task HandleAsync(GerarTokenRequest req, CancellationToken ct)
    {
        var saida = await casoDeUso.ExecutarAsync(new GerarTokenEntrada(req.Usuario, req.Senha), ct);
        await Send.OkAsync(new GerarTokenResponse(saida.AccessToken, saida.ExpiraEm), ct);
    }
}
