using Pedidos.Application.Abstracoes;

namespace Pedidos.Application.CasosDeUso.Sistema.ObterInformacoes;

public sealed class ObterInformacoesUseCase(IInformacoesDaAplicacao informacoes)
    : IUseCase<ObterInformacoesEntrada, ObterInformacoesSaida>
{
    public Task<ObterInformacoesSaida> ExecutarAsync(ObterInformacoesEntrada entrada, CancellationToken ct) =>
        Task.FromResult(new ObterInformacoesSaida(informacoes.Nome, informacoes.Versao, informacoes.Ambiente));
}
