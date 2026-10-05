using Pedidos.Application.Abstracoes;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.Application.CasosDeUso.Clientes.ObterCliente;

public sealed class ObterClienteUseCase(IClienteRepository clientes) : IUseCase<ObterClienteEntrada, ObterClienteSaida>
{
    public async Task<ObterClienteSaida> ExecutarAsync(ObterClienteEntrada entrada, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var cliente = await clientes.ObterAsync(entrada.Id, ct) ?? throw new NaoEncontradoException("Cliente", entrada.Id);
        return new ObterClienteSaida(cliente.Id, cliente.Nome, cliente.Email, cliente.CriadoEm);
    }
}
