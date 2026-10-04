using Moq;
using Pedidos.Application.Abstracoes;
using Pedidos.Application.CasosDeUso.Sistema.ObterInformacoes;

namespace Pedidos.UnitTests.CasosDeUso.Sistema;

/// <summary>Requisito: Informações da API.</summary>
public class ObterInformacoesUseCaseTests
{
    [Fact]
    public async Task ExecutarAsync_InformacoesDaAplicacao_RetornaNomeVersaoEAmbiente()
    {
        var informacoes = new Mock<IInformacoesDaAplicacao>();
        informacoes.SetupGet(i => i.Nome).Returns("API de Pedidos");
        informacoes.SetupGet(i => i.Versao).Returns("1.2.3");
        informacoes.SetupGet(i => i.Ambiente).Returns("Development");
        var casoDeUso = new ObterInformacoesUseCase(informacoes.Object);

        var saida = await casoDeUso.ExecutarAsync(new ObterInformacoesEntrada(), CancellationToken.None);

        Assert.Equal(new ObterInformacoesSaida("API de Pedidos", "1.2.3", "Development"), saida);
    }
}
