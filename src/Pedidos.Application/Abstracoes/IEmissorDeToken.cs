namespace Pedidos.Application.Abstracoes;

/// <summary>Token de acesso emitido e sua validade em segundos a partir da emissão.</summary>
public sealed record TokenEmitido(string AccessToken, int ExpiraEmSegundos);

/// <summary>Emite tokens de acesso para um usuário já autenticado.</summary>
public interface IEmissorDeToken
{
    TokenEmitido Emitir(string usuario);
}
