## 1. Domain

- [x] 1.1 Entidade `Cliente` com `Criar(nome, email, agora)`, `Atualizar(nome, email)` e `Restaurar(...)`: `Trim`, nome 1–150, e-mail com formato básico e até 200, `RegraDeNegocioException` em dado inválido, id `Guid.CreateVersion7()` e constantes de tamanho públicas; verificar com testes unitários da entidade (normalização, limites e e-mail inválido)
- [x] 1.2 Interface `IClienteRepository` (obter, existe e-mail ignorando um id, inserir, atualizar, excluir, listar paginado com busca); verificar compilação e uso em mocks

## 2. Persistência

- [x] 2.1 Script `0001_criar_clientes.sql` com a tabela e o índice único em `lower(email)` (ver design.md); verificar no teste de integração que a migração aplica em banco vazio e que o índice existe
- [x] 2.2 `ClienteRepository` (Dapper, via `IDbSession` e transação): listagem com `count(*) OVER()`, `ORDER BY nome, id` e `ILIKE ... ESCAPE '\'` com `\`, `%` e `_` escapados; tradução de `23505` em `ux_clientes_email` para `ConflitoException`; verificar com testes de integração do repositório (busca literal com `%`, ordem estável e conflito no índice)

## 3. Casos de uso

- [x] 3.1 `CriarClienteUseCase` e `AtualizarClienteUseCase` com `Saida` próprias (e-mail existente → `ConflitoException`, próprio e-mail não conflita, inexistente → `NaoEncontradoException`, `criadoEm` via `IRelogio`, transação via `IUnitOfWork` com rollback em falha); verificar com `<Nome>UseCaseTests`
- [x] 3.2 `ObterClienteUseCase`, `ListarClientesUseCase` (`ListarClientesSaida` envolvendo `PaginaResultado<ClienteResumo>`) e `ExcluirClienteUseCase` (`Vazio`; inexistente → `NaoEncontradoException`); verificar com `<Nome>UseCaseTests`

## 4. Api

- [x] 4.1 Cinco endpoints em `Endpoints/Clientes/` com `Group<ApiV1>()`, `Validator` (nome, e-mail usando as constantes da entidade, paginação compartilhada), `Summary` e `CreatedAtAsync<ObterClienteEndpoint>` na criação; verificar com testes unitários dos validators, com o teste de rotas registradas (todos sob `/api/v1` e nenhum anônimo) e no Swagger

## 5. Testes de integração

- [x] 5.1 Fluxo CRUD com token: criar (`201` + `Location`), espaços nas pontas, duplicado (`409`), e-mail inválido (`400`), obter/inexistente (`200`/`404`), busca, busca com `%`, envelope de paginação, nomes repetidos entre páginas, atualizar/inexistente (`200`/`404`), próprio e-mail com outra caixa, excluir (`204`/`404`) e acesso sem token (`401`); verificar com `dotnet test`
- [x] 5.2 Duplicidade concorrente: dois `POST` simultâneos com `Ana@x.com` e `ana@x.com` → um `201` e um `409`; verificar com `dotnet test` repetido sem intermitência

## 6. Documentação

- [x] 6.1 Exemplos de clientes no `requests.http` (usando o token) e cenários manuais (se houver) no README; verificar executando os exemplos contra o container
