## 1. Configuração

- [x] 1.1 Opções JWT e de credenciais lidas das variáveis de ambiente, com validação no startup (chave >= 32); verificar que a aplicação encerra com código diferente de zero sem `JWT_CHAVE` e com chave curta
- [x] 1.2 Acrescentar as seis variáveis ao `.env.example` e ao `docker-compose.yml`; verificar `docker compose up` com o `.env` recopiado
- [x] 1.3 `ApiFactory` dos testes de integração com `JWT_*` e `AUTH_*` de teste; verificar que os 20 testes da fundação continuam passando e que um factory sem `JWT_CHAVE` falha ao subir

## 2. Application e Infrastructure

- [x] 2.1 `CredenciaisInvalidasException` em `Application/Excecoes` mapeada para `401` no `MapeamentoDeExcecoes`; verificar com teste unitário do mapeamento
- [x] 2.2 Abstrações `IEmissorDeToken` e `IValidadorDeCredenciais` na Application e implementações em `Infrastructure/Seguranca` (`JsonWebTokenHandler`; comparação por SHA-256 com `FixedTimeEquals`); verificar com testes que decodificam o token (emissor, audiência, expiração) e que comparam credenciais certas, erradas e de tamanhos diferentes
- [x] 2.3 `GerarTokenUseCase` com `Entrada`/`Saida` próprias (`expiraEm` = minutos × 60); verificar com `GerarTokenUseCaseTests` cobrindo credenciais válidas e `CredenciaisInvalidasException`

## 3. Api

- [x] 3.1 Grupo `ApiV1` (prefixo `api/v1`) e autenticação JWT Bearer com `ClockSkew = 0` validando emissor, audiência, assinatura e expiração; verificar que `/info` e `/health/ready` continuam em suas rotas e anônimos
- [x] 3.2 Endpoint `POST /api/v1/auth/token` com `Group<ApiV1>`, `AllowAnonymous`, `Validator` e `Summary`; verificar com teste unitário do validator (campos ausentes)
- [x] 3.3 Swagger: verificar no documento OpenAPI que endpoints protegidos declaram o requisito Bearer e que `auth/token` e `/info` não o declaram (o esquema `JWTBearerAuth` já é publicado pelo `FastEndpoints.Swagger`)

## 4. Testes

- [x] 4.1 Teste de integração sobre o `EndpointDataSource`: nenhuma rota `/api/v1/*` além de `auth/token` com metadado `IAllowAnonymous`; verificar que falha ao adicionar `AllowAnonymous` em um endpoint de teste sob `/api/v1`
- [x] 4.2 Teste sobre as rotas registradas: todo endpoint de `Pedidos.Api.Endpoints` fora de `Sistema` está sob `/api/v1` (`Group<ApiV1>`); verificar com dados de rota conforme e sem grupo
- [x] 4.3 Integração: token válido (`200`, `expiraEm` correto), credenciais inválidas (`401` `ProblemDetails`), campos ausentes (`400`), rota sem versão (`404`) e endpoint de teste protegido sem token (`401` `problem+json`), com token expirado e adulterado (`401`) e com token válido (`200`); verificar com `dotnet test`

## 5. Documentação

- [x] 5.1 README: como obter o token, variáveis de segurança e limitação do usuário único; criar `requests.http` com o exemplo de obtenção do token; verificar executando o exemplo contra o container
