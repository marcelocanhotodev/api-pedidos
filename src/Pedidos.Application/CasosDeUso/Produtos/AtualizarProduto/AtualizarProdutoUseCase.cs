using Pedidos.Application.Abstracoes;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.Application.CasosDeUso.Produtos.AtualizarProduto;

public sealed class AtualizarProdutoUseCase(IProdutoRepository produtos, IUnitOfWork unitOfWork)
    : IUseCase<AtualizarProdutoEntrada, AtualizarProdutoSaida>
{
    public Task<AtualizarProdutoSaida> ExecutarAsync(AtualizarProdutoEntrada entrada, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        return unitOfWork.EmTransacaoAsync(
            async () =>
            {
                var p = await produtos.ObterAsync(entrada.Id, ct) ?? throw new NaoEncontradoException("Produto", entrada.Id);
                p.Atualizar(entrada.Nome, entrada.Preco);

                // Grava só nome e preço; o estoque devolvido é o lido nesta transação.
                await produtos.AtualizarNomeEPrecoAsync(p, ct);
                return new AtualizarProdutoSaida(p.Id, p.Sku, p.Nome, p.Preco, p.Estoque, p.CriadoEm);
            },
            ct);
    }
}
