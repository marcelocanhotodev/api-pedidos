namespace Pedidos.Domain.Excecoes;

/// <summary>
/// Base das exceções que representam situações previstas pelo domínio.
/// A mensagem é segura para ser exibida ao cliente da API.
/// </summary>
public abstract class DominioException : Exception
{
    protected DominioException(string mensagem)
        : base(mensagem)
    {
    }
}
