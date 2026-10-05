using Pedidos.Application.Abstracoes;
using Pedidos.Domain.Entidades;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.Application.CasosDeUso.Produtos.CriarProduto;

public sealed class CriarProdutoUseCase(IProdutoRepository produtos, IUnitOfWork unitOfWork, IRelogio relogio)
    : IUseCase<CriarProdutoEntrada, CriarProdutoSaida>
{
    public Task<CriarProdutoSaida> ExecutarAsync(CriarProdutoEntrada entrada, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var produto = Produto.Criar(entrada.Sku, entrada.Nome, entrada.Preco, entrada.Estoque, relogio.AgoraUtc);

        return unitOfWork.EmTransacaoAsync(
            async () =>
            {
                if (await produtos.ExisteSkuAsync(produto.Sku, ct))
                {
                    throw new ConflitoException($"Já existe um produto com o SKU '{produto.Sku}'.");
                }

                var p = await produtos.InserirAsync(produto, ct);
                return new CriarProdutoSaida(p.Id, p.Sku, p.Nome, p.Preco, p.Estoque, p.CriadoEm);
            },
            ct);
    }
}
