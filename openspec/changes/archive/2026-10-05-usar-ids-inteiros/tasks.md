## 1. Banco

- [x] 1.1 Migração `0002_clientes_id_inteiro.sql` recriando `clientes` com `id integer generated always as identity primary key`, copiando os dados em ordem de `criado_em, id`, recriando `ux_clientes_email` e a constraint `clientes_pkey` (ver design.md); verificar com teste de integração que aplica `0000`–`0001`, insere clientes com UUID, aplica `0002` e confere que todos permanecem com ids inteiros únicos na ordem de cadastro, colunas e índice iguais aos de um banco novo
- [x] 1.2 Teste do esquema: `id` é `integer` identity `ALWAYS` e um `INSERT` informando `id` é rejeitado; verificar com teste de integração

## 2. Domain e Infrastructure

- [x] 2.1 `Cliente.Id` como `int` (0 antes de persistir), sem `Guid.CreateVersion7()`; `Restaurar` recebe `int`; verificar com os testes unitários da entidade atualizados
- [x] 2.2 `IClienteRepository` com ids `int` e `InserirAsync` devolvendo o `Cliente` persistido via `INSERT ... RETURNING id`; `ClienteRepository` e consultas atualizados; verificar com os testes de integração do repositório (id gerado devolvido, ids crescentes)

## 3. Application e Api

- [x] 3.1 Entradas e saídas dos cinco casos de uso de clientes com `int`; `CriarClienteUseCase` usa o cliente devolvido pelo repositório; verificar com os `<Nome>UseCaseTests` atualizados
- [x] 3.2 Requests, `ClienteResponse` e rotas de clientes com `int`; id não numérico → `400` com `errors.id`; `Location` com o id inteiro; verificar com testes de integração (`/clientes/abc` → `400`, `/clientes/999999` → `404`, `"id"` numérico no JSON)

## 4. Testes existentes

- [x] 4.1 Ajustar os testes unitários e de integração de clientes (ids `Guid` → `int`, rotas fixas com UUID → inteiros) e rodar a suíte completa; verificar com `dotnet test` (unitários e integração verdes)

## 5. Planejamento e documentação

- [x] 5.1 Atualizar `openspec/project.md` (convenção de chaves) e os artefatos de `adicionar-produtos`, `adicionar-pedidos` e `adicionar-relatorios` para ids `integer`, chaves estrangeiras inteiras e migrações `0003_criar_produtos`/`0004_criar_pedidos`; verificar com `openspec validate --strict` em cada change e busca sem ocorrências de `uuid`/`Guid`/`v7` fora do histórico arquivado
- [x] 5.2 README (decisões e exemplos com ids numéricos) e `requests.http` (`@clienteId` numérico); verificar subindo o container (a `0002` converte o banco existente) e executando os exemplos
