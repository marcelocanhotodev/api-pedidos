using Pedidos.Application.Comum;

namespace Pedidos.Application.CasosDeUso.Produtos.ListarProdutos;

/// <param name="Busca">Trecho do nome ou do SKU, sem diferenciar maiúsculas; nulo lista todos.</param>
public sealed record ListarProdutosEntrada(string? Busca, Paginacao Paginacao);
