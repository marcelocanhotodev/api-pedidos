using Pedidos.Application.Abstracoes;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.Application.CasosDeUso.Clientes.AtualizarCliente;

public sealed class AtualizarClienteUseCase(IClienteRepository clientes, IUnitOfWork unitOfWork)
    : IUseCase<AtualizarClienteEntrada, AtualizarClienteSaida>
{
    public Task<AtualizarClienteSaida> ExecutarAsync(AtualizarClienteEntrada entrada, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        return unitOfWork.EmTransacaoAsync(
            async () =>
            {
                var cliente = await clientes.ObterAsync(entrada.Id, ct) ?? throw new NaoEncontradoException("Cliente", entrada.Id);
                cliente.Atualizar(entrada.Nome, entrada.Email);

                if (await clientes.ExisteEmailAsync(cliente.Email, ignorarId: cliente.Id, ct))
                {
                    throw new ConflitoException($"Já existe um cliente com o e-mail '{cliente.Email}'.");
                }

                await clientes.AtualizarAsync(cliente, ct);
                return new AtualizarClienteSaida(cliente.Id, cliente.Nome, cliente.Email, cliente.CriadoEm);
            },
            ct);
    }
}
