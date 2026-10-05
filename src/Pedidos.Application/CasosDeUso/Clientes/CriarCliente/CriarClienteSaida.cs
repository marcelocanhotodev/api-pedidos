namespace Pedidos.Application.CasosDeUso.Clientes.CriarCliente;

public sealed record CriarClienteSaida(Guid Id, string Nome, string Email, DateTimeOffset CriadoEm);
