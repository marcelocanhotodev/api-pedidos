using System.Security.Cryptography;
using System.Text;
using Pedidos.Application.Abstracoes;

namespace Pedidos.Infrastructure.Seguranca;

/// <summary>
/// Compara o SHA-256 das credenciais informadas com o das configuradas em tempo constante:
/// o tempo de resposta não revela nem o conteúdo nem o tamanho da senha.
/// </summary>
internal sealed class ValidadorDeCredenciais(OpcoesDeSeguranca opcoes) : IValidadorDeCredenciais
{
    private readonly byte[] _hashUsuario = Hash(opcoes.Usuario);
    private readonly byte[] _hashSenha = Hash(opcoes.Senha);

    public bool Validar(string usuario, string senha)
    {
        var usuarioConfere = CryptographicOperations.FixedTimeEquals(Hash(usuario ?? string.Empty), _hashUsuario);
        var senhaConfere = CryptographicOperations.FixedTimeEquals(Hash(senha ?? string.Empty), _hashSenha);
        return usuarioConfere & senhaConfere;
    }

    private static byte[] Hash(string valor) => SHA256.HashData(Encoding.UTF8.GetBytes(valor));
}
