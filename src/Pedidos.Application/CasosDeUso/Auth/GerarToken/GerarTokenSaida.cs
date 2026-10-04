namespace Pedidos.Application.CasosDeUso.Auth.GerarToken;

/// <param name="AccessToken">JWT a ser enviado no cabeçalho <c>Authorization: Bearer</c>.</param>
/// <param name="ExpiraEm">Validade do token, em segundos a partir da emissão.</param>
public sealed record GerarTokenSaida(string AccessToken, int ExpiraEm);
