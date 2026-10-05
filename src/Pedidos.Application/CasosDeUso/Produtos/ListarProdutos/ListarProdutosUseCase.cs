using Pedidos.Application.Abstracoes;
using Pedidos.Application.Comum;
using Pedidos.Domain.Repositorios;

namespace Pedidos.Application.CasosDeUso.Produtos.ListarProdutos;

public sealed class ListarProdutosUseCase(IProdutoRepository produtos) : IUseCase<ListarProdutosEntrada, ListarProdutosSaida>
{
    public async Task<ListarProdutosSaida> ExecutarAsync(ListarProdutosEntrada entrada, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var busca = string.IsNullOrWhiteSpace(entrada.Busca) ? null : entrada.Busca.Trim();
        var resultado = await produtos.ListarAsync(busca, entrada.Paginacao.Deslocamento, entrada.Paginacao.TamanhoPagina, ct);

        var itens = resultado.Itens.Select(p => new ProdutoResumo(p.Id, p.Sku, p.Nome, p.Preco, p.Estoque, p.CriadoEm)).ToList();
        return new ListarProdutosSaida(PaginaResultado.Criar(itens, entrada.Paginacao, resultado.Total));
    }
}
