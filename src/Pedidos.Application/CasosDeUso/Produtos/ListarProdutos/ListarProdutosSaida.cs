using Pedidos.Application.Comum;

namespace Pedidos.Application.CasosDeUso.Produtos.ListarProdutos;

public sealed record ProdutoResumo(int Id, string Sku, string Nome, decimal Preco, int Estoque, DateTimeOffset CriadoEm);

public sealed record ListarProdutosSaida(PaginaResultado<ProdutoResumo> Pagina);
