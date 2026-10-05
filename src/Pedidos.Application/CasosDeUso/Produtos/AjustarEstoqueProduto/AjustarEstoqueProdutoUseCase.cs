using Pedidos.Application.Abstracoes;
using Pedidos.Domain.Entidades;
using Pedidos.Domain.Repositorios;

namespace Pedidos.Application.CasosDeUso.Produtos.AjustarEstoqueProduto;

public sealed class AjustarEstoqueProdutoUseCase(IProdutoRepository produtos, IUnitOfWork unitOfWork)
    : IUseCase<AjustarEstoqueProdutoEntrada, AjustarEstoqueProdutoSaida>
{
    public Task<AjustarEstoqueProdutoSaida> ExecutarAsync(AjustarEstoqueProdutoEntrada entrada, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        // Regra pura do delta na entidade; a soma é aplicada de forma atômica pelo repositório.
        Produto.ValidarDelta(entrada.Delta);

        return unitOfWork.EmTransacaoAsync(
            async () =>
            {
                var p = await produtos.AjustarEstoqueAsync(entrada.Id, entrada.Delta, ct);
                return new AjustarEstoqueProdutoSaida(p.Id, p.Sku, p.Nome, p.Preco, p.Estoque, p.CriadoEm);
            },
            ct);
    }
}
