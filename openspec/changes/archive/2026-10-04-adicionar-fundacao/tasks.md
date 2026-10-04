## 1. Estrutura da solução

- [x] 1.1 Criar solução `Pedidos` com Domain, Application, Infrastructure, Api, UnitTests e IntegrationTests (.NET 10); verificar com `dotnet build`
- [x] 1.2 Configurar referências respeitando a regra de dependência, `nullable` e *warnings as errors* em `Directory.Build.props`; verificar que um warning proposital quebra o build
- [x] 1.3 Adicionar `.editorconfig`, `.gitignore` e `.dockerignore`; verificar que `bin/`, `obj/` e `.env` ficam fora do contexto de build

## 2. Domain e Application (base)

- [x] 2.1 Exceções de domínio `NaoEncontradoException`, `ConflitoException` e `RegraDeNegocioException`; verificar com testes unitários
- [x] 2.2 Contrato `IUseCase<TEntrada, TSaida>`, tipo `Vazio`, `IRelogio`, `IUnitOfWork` e registro por varredura em `AddApplication`; verificar com teste que resolve do contêiner de DI um caso de uso definido no projeto de testes
- [x] 2.3 Modelo de paginação compartilhado (entrada `pagina`/`tamanhoPagina` e envelope `itens`, `totalItens`, `totalPaginas`) e regra de validação reutilizável pelos validators; verificar com testes unitários de padrões, limites e cálculo de `totalPaginas`

## 3. Persistência (Infrastructure)

- [x] 3.1 `NpgsqlDataSource`, `IDbSession` e `UnitOfWork` com escopo por requisição, e `MatchNamesWithUnderscores`; verificar commit e rollback em teste de integração
- [x] 3.2 Executor DbUp no startup controlado por `APLICAR_MIGRACOES`, lendo scripts embutidos em `Migracoes/Scripts` (nenhum script de tabela nesta fatia); verificar que `schemaversions` é criada e que `APLICAR_MIGRACOES=false` não altera o esquema
- [x] 3.3 Espera pelo banco com *retry* por até 30 s; verificar subida com o PostgreSQL atrasado e encerramento com código diferente de zero quando ele não sobe

## 4. Api (FastEndpoints)

- [x] 4.1 Configuração do FastEndpoints, Swagger em `/swagger` e Serilog JSON no stdout; verificar que `/swagger` carrega e que uma requisição gera log com método, caminho, status e tempo
- [x] 4.2 Tratador global de exceções com `ProblemDetails` (404/409/422/500, sem stack trace); verificar com testes do mapeamento de cada exceção
- [x] 4.4 Rota `GET /info` (anônima) com `ObterInformacoesUseCase` e endpoint documentado no Swagger, necessária porque o FastEndpoints não inicia sem endpoints; verificar com `ObterInformacoesUseCaseTests`, teste de integração (`200` e presença no OpenAPI) e no `/swagger` do container
- [x] 4.3 Health checks `/health/live` e `/health/ready`; verificar `200`/`503` com banco disponível e indisponível

## 5. Containerização

- [x] 5.1 `Dockerfile` multi-stage (.NET 10), usuário não-root, porta 8080; verificar `docker build .` em clone limpo e processo sem root
- [x] 5.2 `docker-compose.yml` com `postgres` e `api`, healthchecks e volume nomeado; verificar `docker compose up --build` e `/health/ready` = `200`
- [x] 5.3 `.env.example` com as variáveis da fundação; verificar subida copiando-o para `.env`

## 6. Testes

- [x] 6.1 Teste de arquitetura: camadas, ausência de EF, endpoints só com `IUseCase<,>`, casos de uso sem infraestrutura e convenção de nomes/pastas; verificar que passa e que falha ao introduzir uma violação proposital
- [x] 6.2 Infraestrutura de testes de integração (`WebApplicationFactory` + Testcontainers + migrações); verificar com o teste de `/health/ready` e o de erro `500` como `ProblemDetails`

## 7. Documentação

- [x] 7.1 `README.md` (como rodar, testar, variáveis, decisões, ordem das fatias e cenários verificados manualmente); verificar seguindo o README em clone limpo
