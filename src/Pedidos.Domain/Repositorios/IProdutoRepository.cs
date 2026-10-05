using Pedidos.Domain.Entidades;

namespace Pedidos.Domain.Repositorios;

/// <summary>Página de produtos e o total de itens que atendem ao filtro.</summary>
public sealed record PaginaDeProdutos(IReadOnlyList<Produto> Itens, long Total);

public interface IProdutoRepository
{
    Task<Produto?> ObterAsync(int id, CancellationToken ct);

    /// <summary>Indica se o SKU (já normalizado) está em uso.</summary>
    Task<bool> ExisteSkuAsync(string sku, CancellationToken ct);

    /// <summary>Insere o produto e devolve-o já persistido, com o id gerado pelo banco.</summary>
    /// <exception cref="Excecoes.ConflitoException">O SKU já está em uso (índice único).</exception>
    Task<Produto> InserirAsync(Produto produto, CancellationToken ct);

    /// <summary>Grava apenas nome e preço; SKU e estoque não são tocados.</summary>
    Task AtualizarNomeEPrecoAsync(Produto produto, CancellationToken ct);

    /// <summary>
    /// Soma <paramref name="delta"/> ao estoque numa única instrução atômica e devolve o produto atualizado.
    /// </summary>
    /// <exception cref="Excecoes.NaoEncontradoException">O produto não existe.</exception>
    /// <exception cref="Excecoes.RegraDeNegocioException">O estoque ficaria fora do intervalo permitido.</exception>
    Task<Produto> AjustarEstoqueAsync(int id, int delta, CancellationToken ct);

    /// <summary>Lista ordenada por nome e id; <paramref name="busca"/> filtra nome ou SKU, tratando curingas como literais.</summary>
    Task<PaginaDeProdutos> ListarAsync(string? busca, int deslocamento, int quantidade, CancellationToken ct);
}
