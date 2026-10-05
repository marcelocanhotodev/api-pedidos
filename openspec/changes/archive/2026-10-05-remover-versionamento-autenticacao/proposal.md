# Change: Remover versionamento da rota de autenticação

## Why
A emissão de token não é um recurso de negócio: é infraestrutura de acesso, como `/info` e `/health`, e não deve
mudar quando a API ganhar uma `v2`. Mantê-la em `/api/v1/auth/token` amarra a autenticação à versão do contrato de
negócio e obrigaria os clientes a trocar a URL de login a cada nova versão.

## What Changes
- **BREAKING**: a emissão de token passa de `POST /api/v1/auth/token` para `POST /auth/token`, fora de qualquer versão.
  A rota antiga deixa de existir e passa a responder `404` (premissa: a API ainda não tem consumidores externos, então
  não há período de convivência entre as duas rotas).
- `/api/v1` passa a conter apenas endpoints de negócio, todos protegidos: nenhuma rota sob `/api/v1` aceita acesso anônimo.
- O endpoint de token sai do grupo `ApiV1`; o teste de rotas passa a aceitar endpoints públicos fora do grupo em `Auth` e `Sistema`.
- Swagger, README e `requests.http` passam a mostrar `POST /auth/token`.

## Capabilities

### New Capabilities
_Nenhuma._

### Modified Capabilities
- `seguranca`: "Emissão de Token" muda a rota para `POST /auth/token`; "Proteção dos Endpoints" passa a exigir token em todo `/api/v1`, sem exceção
- `convencoes-api`: "Versionamento da API" ganha a exceção explícita da autenticação e troca os exemplos; "Documentação da API" troca a rota de token no cenário de segurança

## Impact
- Código: `Api/Endpoints/Auth/GerarTokenEndpoint.cs` (rota e grupo).
- Testes: regras de rotas registradas (`RegrasDeRotas`), `SegurancaTests`, helper de autenticação dos testes de integração.
- Documentação: `README.md` e `requests.http`.
- Clientes da API: quem chama `POST /api/v1/auth/token` precisa passar a chamar `POST /auth/token`; os demais endpoints não mudam.
