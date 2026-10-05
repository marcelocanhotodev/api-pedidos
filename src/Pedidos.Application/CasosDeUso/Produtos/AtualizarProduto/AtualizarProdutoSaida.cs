namespace Pedidos.Application.CasosDeUso.Produtos.AtualizarProduto;

public sealed record AtualizarProdutoSaida(int Id, string Sku, string Nome, decimal Preco, int Estoque, DateTimeOffset CriadoEm);
