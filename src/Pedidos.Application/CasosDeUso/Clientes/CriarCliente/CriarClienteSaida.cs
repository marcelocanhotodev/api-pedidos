namespace Pedidos.Application.CasosDeUso.Clientes.CriarCliente;

public sealed record CriarClienteSaida(int Id, string Nome, string Email, DateTimeOffset CriadoEm);
