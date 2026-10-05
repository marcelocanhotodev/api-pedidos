namespace Pedidos.Application.CasosDeUso.Produtos.AjustarEstoqueProduto;

/// <param name="Delta">Quantidade a somar (positiva) ou subtrair (negativa) do estoque.</param>
public sealed record AjustarEstoqueProdutoEntrada(int Id, int Delta);
