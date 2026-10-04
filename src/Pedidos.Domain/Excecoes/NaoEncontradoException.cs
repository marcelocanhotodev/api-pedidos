namespace Pedidos.Domain.Excecoes;

/// <summary>O recurso solicitado não existe.</summary>
public sealed class NaoEncontradoException : DominioException
{
    public NaoEncontradoException(string recurso, object id)
        : base($"{recurso} '{id}' não foi encontrado.")
    {
        Recurso = recurso;
        Id = id;
    }

    public string Recurso { get; }

    public object Id { get; }
}
