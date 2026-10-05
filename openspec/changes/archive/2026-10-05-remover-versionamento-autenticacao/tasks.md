## 1. Api

- [x] 1.1 `GerarTokenEndpoint` com `Post("auth/token")` sem `Group<ApiV1>()`, mantendo `AllowAnonymous`, `Validator` e `Summary`; verificar que `POST /auth/token` responde `200` e `POST /api/v1/auth/token` responde `404`

## 2. Regras de rotas

- [x] 2.1 `RegrasDeRotas`: `AnonimasIndevidas` sem a exceção do token (qualquer rota anônima sob `/api/v1` é violação) e `ForaDoGrupoApiV1` isentando `Auth` e `Sistema`; verificar com os testes de regras usando dados (rota anônima sob `/api/v1` acusada, token fora do grupo aceito, endpoint de negócio sem grupo acusado) e com o teste sobre as rotas reais

## 3. Testes de integração

- [x] 3.1 `SegurancaTests` e helper de autenticação apontando para `POST /auth/token`; casos novos: rota versionada removida (`404`), token sem autenticação nas rotas públicas e `GET /clientes` sem versão (`404`); verificar com `dotnet test`
- [x] 3.2 Swagger: `POST /auth/token` documentado sem requisito Bearer e ausência de `/api/v1/auth/token` no OpenAPI; verificar com o teste do documento OpenAPI e no `/swagger` do container

## 4. Documentação

- [x] 4.1 README (tabela de URLs, seção de autenticação e decisões) e `requests.http` com `POST /auth/token`; verificar executando o exemplo de token contra o container
