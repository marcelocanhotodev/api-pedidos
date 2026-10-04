using FastEndpoints;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Sistema.ObterInformacoes;

namespace Pedidos.Api.Endpoints.Sistema;

public sealed record ObterInformacoesResponse(string Nome, string Versao, string Ambiente);

internal sealed class ObterInformacoesEndpoint(IUseCase<ObterInformacoesEntrada, ObterInformacoesSaida> casoDeUso)
    : EndpointWithoutRequest<ObterInformacoesResponse>
{
    public override void Configure()
    {
        Get("/info");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Informações da API";
            s.Description = "Retorna nome, versão e ambiente da API em execução.";
            s.Responses[StatusCodes.Status200OK] = "Informações da API.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var saida = await casoDeUso.ExecutarAsync(new ObterInformacoesEntrada(), ct);
        await Send.OkAsync(new ObterInformacoesResponse(saida.Nome, saida.Versao, saida.Ambiente), ct);
    }
}
