## 1. Domain e persistência

- [ ] 1.1 Entidade `Cliente` e interface `IClienteRepository` no Domain; verificar com testes unitários da entidade
- [ ] 1.2 Script `0001_criar_clientes.sql`: `id uuid pk`, `nome varchar(150) not null`, `email varchar(200) not null`, `criado_em timestamptz not null`, índice único em `lower(email)`; verificar que a migração aplica em banco vazio no teste de integração
- [ ] 1.3 `ClienteRepository` (Dapper) com busca `ILIKE` paginada e tradução da violação de unicidade em `ConflitoException`; verificar com testes de integração do repositório

## 2. Casos de uso

- [ ] 2.1 `CriarClienteUseCase` e `AtualizarClienteUseCase` (e-mail duplicado → conflito, `criadoEm` via `IRelogio`); verificar com `<Nome>UseCaseTests`
- [ ] 2.2 `ObterClienteUseCase`, `ListarClientesUseCase` e `ExcluirClienteUseCase` (inexistente → não encontrado); verificar com `<Nome>UseCaseTests`

## 3. Api

- [ ] 3.1 Cinco endpoints em `Endpoints/Clientes/` com `Validator` (nome, e-mail, paginação) e `Summary`; verificar com testes unitários dos validators e no Swagger

## 4. Testes de integração

- [ ] 4.1 Fluxo CRUD com token: criar (`201` + `Location`), duplicado (`409`), e-mail inválido (`400`), obter/inexistente (`200`/`404`), busca, envelope de paginação, atualizar, excluir (`204`/`404`) e acesso sem token (`401`); verificar com `dotnet test`

## 5. Documentação

- [ ] 5.1 Exemplos de clientes no `requests.http` e cenários manuais (se houver) no README; verificar executando os exemplos
