namespace Pedidos.Domain.Excecoes;

/// <summary>A operação viola uma regra de negócio (ex.: estoque insuficiente, transição de status inválida).</summary>
public sealed class RegraDeNegocioException : DominioException
{
    public RegraDeNegocioException(string mensagem)
        : base(mensagem)
    {
    }
}
