namespace Pedidos.Application.CasosDeUso.Clientes.ObterCliente;

public sealed record ObterClienteSaida(int Id, string Nome, string Email, DateTimeOffset CriadoEm);
