using System.Reflection;
using Pedidos.UnitTests.Arquitetura.Fixtures.CasosDeUso.Exemplos.ObterExemplo;
using Pedidos.UnitTests.Arquitetura.Fixtures.Violacoes;

namespace Pedidos.UnitTests.Arquitetura;

/// <summary>
/// Requisitos: Estrutura em Camadas, Convenção dos Casos de Uso, Regras dos Casos de Uso,
/// Um Caso de Uso por Endpoint e Teste de Arquitetura.
/// </summary>
public class ArquiteturaTests
{
    private const string RaizDosCasosDeUso = "Pedidos.Application.CasosDeUso";
    private const string RaizDasFixtures = "Pedidos.UnitTests.Arquitetura.Fixtures.CasosDeUso";

    private static readonly Assembly Domain = typeof(Pedidos.Domain.Excecoes.DominioException).Assembly;
    private static readonly Assembly Application = typeof(Pedidos.Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(Pedidos.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    public static TheoryData<string> AssembliesDeProducao => ["Pedidos.Domain", "Pedidos.Application", "Pedidos.Infrastructure", "Pedidos.Api"];

    // --- Regra de dependência entre camadas ---

    [Fact]
    public void Domain_NaoReferenciaOutrasCamadasNemInfraestrutura()
    {
        Assert.Empty(RegrasDeArquitetura.ReferenciasProibidas(Domain, "Pedidos.Application", "Pedidos.Infrastructure", "Pedidos.Api"));
        Assert.Empty(RegrasDeArquitetura.ReferenciasAPacotesDeInfraestrutura(Domain));
    }

    [Fact]
    public void Application_ReferenciaApenasDomain()
    {
        Assert.Empty(RegrasDeArquitetura.ReferenciasProibidas(Application, "Pedidos.Infrastructure", "Pedidos.Api"));
        Assert.Empty(RegrasDeArquitetura.ReferenciasAPacotesDeInfraestrutura(Application));
    }

    [Fact]
    public void Infrastructure_NaoReferenciaApi()
    {
        Assert.Empty(RegrasDeArquitetura.ReferenciasProibidas(Infrastructure, "Pedidos.Api"));
    }

    [Fact]
    public void ReferenciasProibidas_CamadaReferenciandoCamadaExterna_DetectaViolacao()
    {
        // O próprio assembly de testes referencia a Api: serve de prova de que a regra detecta a violação.
        var violacoes = RegrasDeArquitetura.ReferenciasProibidas(typeof(ArquiteturaTests).Assembly, "Pedidos.Api");

        Assert.Single(violacoes);
    }

    [Theory]
    [MemberData(nameof(AssembliesDeProducao))]
    public void Solucao_NaoReferenciaEntityFramework(string nomeDoAssembly)
    {
        var assembly = new[] { Domain, Application, Infrastructure, Api }.Single(a => a.GetName().Name == nomeDoAssembly);

        Assert.Empty(RegrasDeArquitetura.ReferenciasAEntityFramework(assembly));
    }

    // --- Convenção dos casos de uso ---

    [Fact]
    public void CasosDeUso_SeguemConvencaoDeNomesEPastas()
    {
        var casosDeUso = RegrasDeArquitetura.CasosDeUso(Application);

        Assert.Empty(RegrasDeArquitetura.ViolacoesDeConvencao(casosDeUso, RaizDosCasosDeUso));
    }

    [Fact]
    public void ViolacoesDeConvencao_CasoDeUsoConforme_NaoAcusa()
    {
        Assert.Empty(RegrasDeArquitetura.ViolacoesDeConvencao([typeof(ObterExemploUseCase)], RaizDasFixtures));
    }

    [Theory]
    [InlineData(typeof(CasoSemSufixo))]
    [InlineData(typeof(ForaDaPastaUseCase))]
    public void ViolacoesDeConvencao_ForaDaConvencao_DetectaViolacao(Type casoDeUso)
    {
        Assert.NotEmpty(RegrasDeArquitetura.ViolacoesDeConvencao([casoDeUso], RaizDasFixtures));
    }

    // --- Casos de uso sem infraestrutura e sem dependência entre si ---

    [Fact]
    public void CasosDeUso_NaoDependemDeInfraestruturaNemDeOutrosCasosDeUso()
    {
        var casosDeUso = RegrasDeArquitetura.CasosDeUso(Application);

        Assert.Empty(RegrasDeArquitetura.ViolacoesDeDependenciaDosCasosDeUso(casosDeUso));
    }

    [Theory]
    [InlineData(typeof(ComNpgsqlUseCase))]
    [InlineData(typeof(ComHttpContextUseCase))]
    [InlineData(typeof(DependeDeOutroUseCase))]
    public void ViolacoesDeDependencia_CasoDeUsoComDependenciaProibida_DetectaViolacao(Type casoDeUso)
    {
        Assert.NotEmpty(RegrasDeArquitetura.ViolacoesDeDependenciaDosCasosDeUso([casoDeUso]));
    }

    // --- Endpoints finos: um endpoint, um caso de uso ---

    [Fact]
    public void Endpoints_RecebemApenasUmCasoDeUsoECadaCasoDeUsoTemUmEndpoint()
    {
        var violacoes = RegrasDeArquitetura.ViolacoesDosEndpoints(
            RegrasDeArquitetura.Endpoints(Api),
            RegrasDeArquitetura.CasosDeUso(Application));

        Assert.Empty(violacoes);
    }

    [Fact]
    public void ViolacoesDosEndpoints_EndpointConforme_NaoAcusa()
    {
        var violacoes = RegrasDeArquitetura.ViolacoesDosEndpoints([typeof(ObterExemploEndpoint)], [typeof(ObterExemploUseCase)]);

        Assert.Empty(violacoes);
    }

    [Fact]
    public void ViolacoesDosEndpoints_EndpointComUnitOfWork_DetectaViolacao()
    {
        var violacoes = RegrasDeArquitetura.ViolacoesDosEndpoints([typeof(ComUnitOfWorkEndpoint)], []);

        Assert.Contains(violacoes, v => v.Contains("IUnitOfWork", StringComparison.Ordinal));
    }

    [Fact]
    public void ViolacoesDosEndpoints_CasoDeUsoSemEndpoint_DetectaViolacao()
    {
        var violacoes = RegrasDeArquitetura.ViolacoesDosEndpoints([], [typeof(ObterExemploUseCase)]);

        Assert.Single(violacoes);
    }
}
