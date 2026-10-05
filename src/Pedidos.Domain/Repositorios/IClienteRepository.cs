using Pedidos.Domain.Entidades;

namespace Pedidos.Domain.Repositorios;

/// <summary>Página de clientes e o total de itens que atendem ao filtro.</summary>
public sealed record PaginaDeClientes(IReadOnlyList<Cliente> Itens, long Total);

public interface IClienteRepository
{
    Task<Cliente?> ObterAsync(int id, CancellationToken ct);

    /// <summary>Indica se o e-mail (sem diferenciar maiúsculas) já pertence a outro cliente que não <paramref name="ignorarId"/>.</summary>
    Task<bool> ExisteEmailAsync(string email, int? ignorarId, CancellationToken ct);

    /// <summary>Insere o cliente e devolve-o já persistido, com o id gerado pelo banco.</summary>
    /// <exception cref="Excecoes.ConflitoException">O e-mail já está em uso (índice único).</exception>
    Task<Cliente> InserirAsync(Cliente cliente, CancellationToken ct);

    /// <exception cref="Excecoes.ConflitoException">O e-mail já está em uso (índice único).</exception>
    Task AtualizarAsync(Cliente cliente, CancellationToken ct);

    /// <returns><c>false</c> se o cliente não existia.</returns>
    Task<bool> ExcluirAsync(int id, CancellationToken ct);

    /// <summary>Lista ordenada por nome e id; <paramref name="busca"/> filtra nome ou e-mail, tratando curingas como literais.</summary>
    Task<PaginaDeClientes> ListarAsync(string? busca, int deslocamento, int quantidade, CancellationToken ct);
}
