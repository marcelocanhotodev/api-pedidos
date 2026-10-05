namespace Pedidos.Application.CasosDeUso.Produtos.ObterProduto;

public sealed record ObterProdutoSaida(int Id, string Sku, string Nome, decimal Preco, int Estoque, DateTimeOffset CriadoEm);
