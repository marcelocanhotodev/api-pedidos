namespace Pedidos.Application.CasosDeUso.Produtos.AjustarEstoqueProduto;

public sealed record AjustarEstoqueProdutoSaida(int Id, string Sku, string Nome, decimal Preco, int Estoque, DateTimeOffset CriadoEm);
