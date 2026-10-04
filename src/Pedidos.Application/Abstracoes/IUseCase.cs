namespace Pedidos.Application.Abstracoes;

/// <summary>
/// Caso de uso da aplicação: exatamente um por endpoint.
/// Implementações ficam em <c>CasosDeUso/&lt;Area&gt;/&lt;Nome&gt;/</c> e recebem todas as dependências pelo construtor.
/// </summary>
public interface IUseCase<in TEntrada, TSaida>
{
    Task<TSaida> ExecutarAsync(TEntrada entrada, CancellationToken ct);
}
