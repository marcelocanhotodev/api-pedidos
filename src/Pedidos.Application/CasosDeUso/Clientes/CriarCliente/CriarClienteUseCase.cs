using Pedidos.Application.Abstracoes;
using Pedidos.Domain.Entidades;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.Application.CasosDeUso.Clientes.CriarCliente;

public sealed class CriarClienteUseCase(IClienteRepository clientes, IUnitOfWork unitOfWork, IRelogio relogio)
    : IUseCase<CriarClienteEntrada, CriarClienteSaida>
{
    public Task<CriarClienteSaida> ExecutarAsync(CriarClienteEntrada entrada, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var cliente = Cliente.Criar(entrada.Nome, entrada.Email, relogio.AgoraUtc);

        return unitOfWork.EmTransacaoAsync(
            async () =>
            {
                if (await clientes.ExisteEmailAsync(cliente.Email, ignorarId: null, ct))
                {
                    throw new ConflitoException($"Já existe um cliente com o e-mail '{cliente.Email}'.");
                }

                await clientes.InserirAsync(cliente, ct);
                return new CriarClienteSaida(cliente.Id, cliente.Nome, cliente.Email, cliente.CriadoEm);
            },
            ct);
    }
}
