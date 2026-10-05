using Pedidos.Application.Abstracoes;
using Pedidos.Application.Comum;
using Pedidos.Domain.Repositorios;

namespace Pedidos.Application.CasosDeUso.Clientes.ListarClientes;

public sealed class ListarClientesUseCase(IClienteRepository clientes) : IUseCase<ListarClientesEntrada, ListarClientesSaida>
{
    public async Task<ListarClientesSaida> ExecutarAsync(ListarClientesEntrada entrada, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var busca = string.IsNullOrWhiteSpace(entrada.Busca) ? null : entrada.Busca.Trim();
        var resultado = await clientes.ListarAsync(busca, entrada.Paginacao.Deslocamento, entrada.Paginacao.TamanhoPagina, ct);

        var itens = resultado.Itens.Select(c => new ClienteResumo(c.Id, c.Nome, c.Email, c.CriadoEm)).ToList();
        return new ListarClientesSaida(PaginaResultado.Criar(itens, entrada.Paginacao, resultado.Total));
    }
}
