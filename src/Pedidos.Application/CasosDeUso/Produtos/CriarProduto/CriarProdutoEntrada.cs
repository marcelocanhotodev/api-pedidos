namespace Pedidos.Application.CasosDeUso.Produtos.CriarProduto;

public sealed record CriarProdutoEntrada(string Sku, string Nome, decimal Preco, int Estoque);
