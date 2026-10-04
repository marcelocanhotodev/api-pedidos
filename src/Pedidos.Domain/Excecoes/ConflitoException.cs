namespace Pedidos.Domain.Excecoes;

/// <summary>A operação conflita com o estado atual (ex.: valor único já usado, versão desatualizada).</summary>
public sealed class ConflitoException : DominioException
{
    public ConflitoException(string mensagem)
        : base(mensagem)
    {
    }
}
