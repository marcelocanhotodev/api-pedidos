namespace Pedidos.Application.Excecoes;

/// <summary>Usuário ou senha não conferem com as credenciais configuradas.</summary>
public sealed class CredenciaisInvalidasException : Exception
{
    public CredenciaisInvalidasException()
        : base("Usuário ou senha inválidos.")
    {
    }
}
