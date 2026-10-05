namespace Pedidos.Application.CasosDeUso.Clientes.AtualizarCliente;

public sealed record AtualizarClienteSaida(Guid Id, string Nome, string Email, DateTimeOffset CriadoEm);
