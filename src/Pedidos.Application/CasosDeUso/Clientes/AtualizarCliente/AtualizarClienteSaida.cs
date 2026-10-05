namespace Pedidos.Application.CasosDeUso.Clientes.AtualizarCliente;

public sealed record AtualizarClienteSaida(int Id, string Nome, string Email, DateTimeOffset CriadoEm);
