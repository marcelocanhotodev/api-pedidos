using Pedidos.Application.Abstracoes;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.Application.CasosDeUso.Clientes.ExcluirCliente;

public sealed class ExcluirClienteUseCase(IClienteRepository clientes, IUnitOfWork unitOfWork)
    : IUseCase<ExcluirClienteEntrada, Vazio>
{
    public Task<Vazio> ExecutarAsync(ExcluirClienteEntrada entrada, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        return unitOfWork.EmTransacaoAsync(
            async () =>
            {
                if (!await clientes.ExcluirAsync(entrada.Id, ct))
                {
                    throw new NaoEncontradoException("Cliente", entrada.Id);
                }

                return Vazio.Valor;
            },
            ct);
    }
}
