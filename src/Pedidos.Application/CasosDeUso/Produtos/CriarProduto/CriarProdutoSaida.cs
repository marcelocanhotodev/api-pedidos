namespace Pedidos.Application.CasosDeUso.Produtos.CriarProduto;

public sealed record CriarProdutoSaida(int Id, string Sku, string Nome, decimal Preco, int Estoque, DateTimeOffset CriadoEm);
