using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Pedidos.Application.Abstracoes;

namespace Pedidos.Infrastructure.Seguranca;

/// <summary>Emite JWT HS256 com emissor, audiência e expiração configurados.</summary>
internal sealed class EmissorDeTokenJwt(OpcoesDeSeguranca opcoes, IRelogio relogio) : IEmissorDeToken
{
    private static readonly JsonWebTokenHandler Handler = new();

    private readonly SigningCredentials _credenciais = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opcoes.Chave)),
        SecurityAlgorithms.HmacSha256);

    public TokenEmitido Emitir(string usuario)
    {
        var agora = relogio.AgoraUtc.UtcDateTime;
        var validade = TimeSpan.FromMinutes(opcoes.ExpiraMinutos);

        var token = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = opcoes.Emissor,
            Audience = opcoes.Audiencia,
            IssuedAt = agora,
            NotBefore = agora,
            Expires = agora.Add(validade),
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, usuario), new Claim("name", usuario)]),
            SigningCredentials = _credenciais,
        });

        return new TokenEmitido(token, (int)validade.TotalSeconds);
    }
}
