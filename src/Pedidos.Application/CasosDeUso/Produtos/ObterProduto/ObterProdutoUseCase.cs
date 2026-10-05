using Pedidos.Application.Abstracoes;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.Application.CasosDeUso.Produtos.ObterProduto;

public sealed class ObterProdutoUseCase(IProdutoRepository produtos) : IUseCase<ObterProdutoEntrada, ObterProdutoSaida>
{
    public async Task<ObterProdutoSaida> ExecutarAsync(ObterProdutoEntrada entrada, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var p = await produtos.ObterAsync(entrada.Id, ct) ?? throw new NaoEncontradoException("Produto", entrada.Id);
        return new ObterProdutoSaida(p.Id, p.Sku, p.Nome, p.Preco, p.Estoque, p.CriadoEm);
    }
}
