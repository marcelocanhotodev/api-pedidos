using Pedidos.Application.Comum;

namespace Pedidos.Application.CasosDeUso.Clientes.ListarClientes;

/// <param name="Busca">Trecho do nome ou do e-mail, sem diferenciar maiúsculas; nulo lista todos.</param>
public sealed record ListarClientesEntrada(string? Busca, Paginacao Paginacao);
