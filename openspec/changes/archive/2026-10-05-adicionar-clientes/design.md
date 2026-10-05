## Context

Primeira fatia de negócio sobre a fundação e a segurança arquivadas (ver `openspec/specs/`). Já existem: o grupo
`ApiV1` com proteção JWT por padrão, o contrato `IUseCase<,>` com a convenção rígida de `<Nome>Entrada`/`<Nome>Saida`
verificada por teste de arquitetura, `IUnitOfWork`/`IDbSession` por requisição, a paginação compartilhada
(`Paginacao`, `PaginaResultado<T>`, `PaginacaoRequest`) e o tratador global que converte exceções de domínio em 404/409/422.

## Goals / Non-Goals

**Goals:**
- CRUD de clientes que sirva de modelo para produtos e pedidos (endpoint → caso de uso → entidade → repositório).
- E-mail único sem diferenciar maiúsculas, garantido mesmo sob concorrência.

**Non-Goals:**
- Busca textual indexada (`pg_trgm`), soft delete, auditoria de alterações.
- Bloquear exclusão de cliente com pedidos (chega em `adicionar-pedidos`).

## Decisions

- **Saídas próprias por caso de uso.** `CriarClienteSaida`, `ObterClienteSaida` e `AtualizarClienteSaida` têm os mesmos
  campos (`Id`, `Nome`, `Email`, `CriadoEm`) e são tipos distintos; `ExcluirClienteUseCase` devolve `Vazio`;
  `ListarClientesSaida` envolve a página (`PaginaResultado<ClienteResumo>`).
  Segue a convenção arquivada de `arquitetura` sem alterá-la: cada caso de uso evolui sozinho.
  Alternativa descartada: `ClienteSaida` compartilhada — exigiria MODIFIED na convenção e no teste de arquitetura.
- **Entidade `Cliente` como guarda.** `Cliente.Criar(nome, email, agora)` e `cliente.Atualizar(nome, email)` aplicam
  `Trim`, validam tamanho de nome (1–150) e e-mail (formato básico, até 200) e lançam `RegraDeNegocioException` se
  receberem dados inválidos. O validator do endpoint continua respondendo `400` por campo para a entrada HTTP; a entidade
  protege qualquer outro caminho de criação (seed, importação). `Cliente.Restaurar(...)` reconstrói a partir do banco sem validar.
  Alternativa descartada: validar só no validator — Domain anêmico e regras contornáveis fora do HTTP.
- **Unicidade do e-mail em duas camadas.** O caso de uso consulta `IClienteRepository.ExisteEmailAsync(email, ignorarId)`
  e lança `ConflitoException` (caminho comum, testável com mock). O índice único em `lower(email)` cobre a corrida entre
  duas requisições: o repositório traduz `PostgresException` com `SqlState 23505` nesse índice em `ConflitoException`.
- **Busca com curingas escapados.** `busca` vai como parâmetro em `ILIKE '%' || @busca || '%' ESCAPE '\'`, depois de escapar
  `\`, `%` e `_`; assim `busca=%` procura o caractere literal em vez de casar tudo.
- **Ordenação estável.** `ORDER BY nome, id`: com nomes repetidos, o `id` desempata e nenhum item se repete ou some entre páginas.
- **Contagem e página em uma ida ao banco.** `count(*) OVER()` na mesma consulta paginada, evitando dois comandos.
- **Ids `Guid.CreateVersion7()`.** Ordenados no tempo, evitam fragmentar o índice B-tree da chave primária `uuid`.
- **Transação em toda escrita.** Criar, atualizar e excluir usam `IUnitOfWork` mesmo com um único comando, como exige a
  spec de `arquitetura`.
- **`Location` sem string fixa.** O endpoint de criação usa `Send.CreatedAtAsync<ObterClienteEndpoint>` com o `id`,
  resultando em `/api/v1/clientes/{id}`.

## Modelo de dados (script `0001_criar_clientes.sql`)

| Coluna | Tipo |
|--------|------|
| `id` | `uuid primary key` |
| `nome` | `varchar(150) not null` |
| `email` | `varchar(200) not null` |
| `criado_em` | `timestamptz not null` |

Índice: `create unique index ux_clientes_email on clientes (lower(email))`.

## Risks / Trade-offs

- [Três `Saida` com os mesmos campos] → duplicação proposital e restrita à camada de saída; o teste de arquitetura impede atalhos.
- [`ILIKE '%termo%'` não usa índice] → aceitável no volume de uma API de referência; `pg_trgm` fica para quando houver necessidade.
- [Regras de nome/e-mail no validator e na entidade] → mesmas constantes de tamanho usadas pelos dois, definidas na entidade.
