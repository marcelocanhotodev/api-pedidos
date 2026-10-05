# Change: Usar ids inteiros autoincrementais

## Why
O modelo de dados deve seguir um padrão único e familiar para quem consulta o banco: cada tabela com uma chave primária
`id` inteira gerada pelo próprio banco, e cada relação apontando para esse `id`. Hoje `clientes` usa `uuid` gerado na
aplicação, e as fatias planejadas (produtos, pedidos) seguiriam o mesmo caminho. Fazer a troca agora, com apenas uma
tabela implementada e nenhuma chave estrangeira criada, é o momento mais barato.

## What Changes
- **BREAKING**: o `id` de todas as tabelas passa a ser `integer` autoincremental (`GENERATED ALWAYS AS IDENTITY`),
  gerado pelo banco no `INSERT`. A API expõe esse mesmo inteiro: `/api/v1/clientes/42` e `"id": 42` no JSON
  (antes, um UUID em texto).
- Toda relação entre tabelas passa a ser uma chave estrangeira inteira para o `id` da tabela referenciada
  (ex.: `pedidos.cliente_id integer references clientes (id)`).
- Rotas com id não numérico (ex.: `/api/v1/clientes/abc`) respondem `400`.
- Migração nova `0002_clientes_id_inteiro.sql` converte a tabela `clientes` existente **preservando os dados**
  (scripts já aplicados não são editados). As fatias seguintes passam a usar `0003_criar_produtos` e `0004_criar_pedidos`.
- A geração de `Guid.CreateVersion7()` na aplicação é removida; o repositório devolve o id gerado (`RETURNING id`).
- As changes ainda não implementadas (`adicionar-produtos`, `adicionar-pedidos`, `adicionar-relatorios`) e a convenção
  de banco do `project.md` são atualizadas para ids inteiros.

Premissas: `integer` (32 bits, até ~2,1 bilhões de linhas por tabela) atende ao volume da API; ids sequenciais ficam
previsíveis, aceitável porque todo `/api/v1` exige JWT.

## Capabilities

### New Capabilities
_Nenhuma._

### Modified Capabilities
- `persistencia`: novo requisito de chaves primárias inteiras geradas pelo banco e relações por chave estrangeira inteira
- `convencoes-api`: novo requisito de ids numéricos nas rotas e no JSON, com `400` para id não numérico
- `clientes`: "Integridade de Clientes no Banco" passa a exigir `id` inteiro autoincremental e a conversão sem perda dos clientes existentes

## Impact
- Código: entidade `Cliente`, `IClienteRepository`/`ClienteRepository`, entradas e saídas dos casos de uso de clientes,
  endpoints e contratos de clientes, testes unitários e de integração de clientes.
- Banco: migração `0002_clientes_id_inteiro.sql`.
- Clientes da API: `id` deixa de ser string UUID e passa a ser número; URLs antigas com UUID deixam de existir.
- Planejamento: artefatos de `adicionar-produtos`, `adicionar-pedidos` e `adicionar-relatorios`; `openspec/project.md`.
- Precisa ser aplicada antes de `adicionar-produtos`.
