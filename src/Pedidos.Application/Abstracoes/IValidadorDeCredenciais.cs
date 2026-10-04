namespace Pedidos.Application.Abstracoes;

/// <summary>Confere usuário e senha com as credenciais configuradas.</summary>
public interface IValidadorDeCredenciais
{
    bool Validar(string usuario, string senha);
}
