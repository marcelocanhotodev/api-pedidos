// Tipos usados apenas para provar que as regras de arquitetura detectam violações.
// Não são registrados nem executados pela aplicação.
#pragma warning disable CA1812 // Classes nunca instanciadas: existem só para reflexão.

using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Npgsql;
using Pedidos.Application.Abstracoes;

namespace Pedidos.UnitTests.Arquitetura.Fixtures.CasosDeUso.Exemplos.ObterExemplo
{
    public sealed record ObterExemploEntrada(int Id);

    public sealed record ObterExemploSaida(string Nome);

    /// <summary>Caso de uso conforme a convenção.</summary>
    public sealed class ObterExemploUseCase : IUseCase<ObterExemploEntrada, ObterExemploSaida>
    {
        public Task<ObterExemploSaida> ExecutarAsync(ObterExemploEntrada entrada, CancellationToken ct) =>
            Task.FromResult(new ObterExemploSaida("exemplo"));
    }

    /// <summary>Endpoint conforme: recebe exatamente um caso de uso.</summary>
    internal sealed class ObterExemploEndpoint(IUseCase<ObterExemploEntrada, ObterExemploSaida> casoDeUso) : EndpointWithoutRequest
    {
        public override async Task HandleAsync(CancellationToken ct) =>
            await Send.OkAsync(await casoDeUso.ExecutarAsync(new ObterExemploEntrada(1), ct), ct);
    }
}

namespace Pedidos.UnitTests.Arquitetura.Fixtures.Violacoes
{
    using Pedidos.UnitTests.Arquitetura.Fixtures.CasosDeUso.Exemplos.ObterExemplo;

    internal sealed class CasoSemSufixo : IUseCase<int, int>
    {
        public Task<int> ExecutarAsync(int entrada, CancellationToken ct) => Task.FromResult(entrada);
    }

    internal sealed class ForaDaPastaUseCase : IUseCase<ObterExemploEntrada, ObterExemploSaida>
    {
        public Task<ObterExemploSaida> ExecutarAsync(ObterExemploEntrada entrada, CancellationToken ct) =>
            Task.FromResult(new ObterExemploSaida("x"));
    }

    internal sealed class ComNpgsqlUseCase(NpgsqlConnection conexao) : IUseCase<int, int>
    {
        public Task<int> ExecutarAsync(int entrada, CancellationToken ct) => Task.FromResult(conexao.ProcessID);
    }

    internal sealed class ComHttpContextUseCase(IHttpContextAccessor acessor) : IUseCase<int, int>
    {
        public Task<int> ExecutarAsync(int entrada, CancellationToken ct) => Task.FromResult(acessor.GetHashCode());
    }

    internal sealed class DependeDeOutroUseCase(IUseCase<ObterExemploEntrada, ObterExemploSaida> outro) : IUseCase<int, int>
    {
        public async Task<int> ExecutarAsync(int entrada, CancellationToken ct) =>
            (await outro.ExecutarAsync(new ObterExemploEntrada(entrada), ct)).Nome.Length;
    }

    internal sealed class ComUnitOfWorkEndpoint(IUnitOfWork unitOfWork) : EndpointWithoutRequest
    {
        public override Task HandleAsync(CancellationToken ct) => unitOfWork.CommitAsync(ct);
    }
}
