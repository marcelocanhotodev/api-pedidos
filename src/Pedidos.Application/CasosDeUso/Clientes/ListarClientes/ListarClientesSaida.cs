using Pedidos.Application.Comum;

namespace Pedidos.Application.CasosDeUso.Clientes.ListarClientes;

public sealed record ClienteResumo(int Id, string Nome, string Email, DateTimeOffset CriadoEm);

public sealed record ListarClientesSaida(PaginaResultado<ClienteResumo> Pagina);
