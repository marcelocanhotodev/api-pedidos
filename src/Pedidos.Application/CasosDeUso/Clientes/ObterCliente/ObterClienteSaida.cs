namespace Pedidos.Application.CasosDeUso.Clientes.ObterCliente;

public sealed record ObterClienteSaida(Guid Id, string Nome, string Email, DateTimeOffset CriadoEm);
