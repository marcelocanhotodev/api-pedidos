## Context

Fatia sobre a fundação, a segurança e clientes, já arquivadas (ver `openspec/specs/`). Clientes estabeleceu o padrão de
recurso CRUD: grupo `ApiV1` com JWT por padrão, `Entrada`/`Saida` próprias por caso de uso (convenção verificada por
teste de arquitetura), entidade como guarda, `EmTransacaoAsync`, unicidade em duas camadas e busca paginada com
curingas escapados. Produtos segue o mesmo padrão e acrescenta valores numéricos com limites físicos no banco
(`numeric(12,2)`, `int`) e uma operação concorrente: o ajuste de estoque.

## Goals / Non-Goals

**Goals:**
- Nenhuma entrada válida pela API pode causar erro do banco (overflow, arredondamento silencioso) nem `500`.
- Ajustes de estoque simultâneos nunca se perdem nem deixam o estoque fora de `0..1.000.000`.

**Non-Goals:**
- Exclusão ou inativação de produtos (pedidos referenciam produtos; fica para uma change futura).
- Reserva de estoque por pedido (chega em `adicionar-pedidos`, reaproveitando o mesmo mecanismo atômico).
- Busca textual indexada (`pg_trgm`), categorias, imagens, histórico de preços.
- Controle de concorrência em `PUT`: dois `PUT` simultâneos no mesmo produto seguem "a última gravação vence".
  Nome e preço de catálogo não justificam versão/ETag nesta API; pedidos, que têm transições de estado, usam `versao`.

## Decisions

### Padrões herdados de clientes
- **Rotas:** `Group<ApiV1>()` nos cinco endpoints (`/api/v1/produtos`), cobertos pelo teste de rotas registradas.
- **Saídas próprias:** `CriarProdutoSaida`, `ObterProdutoSaida`, `AtualizarProdutoSaida` e `AjustarEstoqueProdutoSaida`
  com os mesmos campos; `ListarProdutosSaida` envolve `PaginaResultado<ProdutoResumo>`.
- **Entidade como guarda:** `Produto.Criar(sku, nome, preco, estoque, agora)`, `produto.Atualizar(nome, preco)` e
  `Produto.Restaurar(...)`; o validator do endpoint reutiliza as constantes e checagens públicas da entidade.
- **Unicidade em duas camadas:** `ExisteSkuAsync` no caso de uso (testável com mock) e índice único `ux_produtos_sku`,
  com `23505` traduzido em `ConflitoException` (`409`) no repositório.
- **Listagem:** `ILIKE ... ESCAPE '\'` em nome ou SKU com `\`, `%` e `_` escapados, `ORDER BY nome, id`,
  total com `count(*) OVER()` (e contagem à parte para página além do fim).
- **Ids e datas:** `id integer` gerado pelo banco e devolvido por `INSERT ... RETURNING` (a entidade nasce com `Id = 0`;
  `InserirAsync` devolve o produto persistido), `criadoEm` via `IRelogio` (microssegundos).
- **Criação:** `Send.CreatedAtAsync<ObterProdutoEndpoint>` para o `Location`.

### Limites numéricos explícitos
`numeric(12,2)` aceita até 9.999.999.999,99 e arredonda casas extras sem avisar; `int` estoura em 2.147.483.647.
Sem limites na aplicação, essas entradas viram erro do banco (`500`) ou um valor diferente do enviado.

| Campo | Regra | Violação |
|-------|-------|----------|
| `preco` | `0 <= preco <= 9.999.999.999,99`, no máximo 2 casas decimais | `400` (validator) / `RegraDeNegocioException` (entidade) |
| `estoque` (inicial e resultante) | `0 <= estoque <= 1.000.000` | `400` na criação; `422` no ajuste |
| `delta` | `delta != 0` e `-1.000.000 <= delta <= 1.000.000` | `400` |

`10.999` é rejeitado em vez de virar `11.00`. Preços válidos são normalizados pela entidade para **sempre 2 casas**
(`4.9` vira `4.90`, por `decimal.Round(preco, 2) + 0.00m`), para que a resposta do `POST`/`PUT` tenha a mesma
representação que o `GET` (o `numeric(12,2)` sempre devolve 2 casas). O banco repete os limites como última barreira:
`check (preco >= 0)` e `check (estoque between 0 and 1000000)`.

### Ajuste de estoque atômico
Ler, alterar a entidade e gravar perde atualizações sob concorrência (duas requisições leem 5, ambas gravam 15).
- A entidade valida só a regra pura do `delta` (`Produto.ValidarDelta`).
- O repositório aplica numa única instrução, com a soma feita em `bigint` para nunca estourar:
  ```sql
  UPDATE produtos SET estoque = estoque + @delta
  WHERE id = @id AND estoque::bigint + @delta BETWEEN 0 AND 1000000
  RETURNING id, sku, nome, preco, estoque, criado_em
  ```
- Zero linhas afetadas: o repositório verifica se o produto existe para distinguir `NaoEncontradoException` (`404`)
  de `RegraDeNegocioException` (`422`, estoque ficaria fora de `0..1.000.000`).
- O `PATCH` devolve o produto com o estoque resultante.
- Alternativa descartada: `SELECT ... FOR UPDATE` seguido de `UPDATE` — duas idas ao banco e lock mais longo, sem ganho.
  O mesmo `UPDATE` condicional será a base da reserva de estoque em `adicionar-pedidos`.

### SKU normalizado e imutável
- `Trim` + maiúsculas (`ToUpperInvariant`), aceitando só `A-Z`, `0-9`, `-`, `_` e `.`, com 1 a 50 caracteres.
  `" abc-1 "` e `"ABC-1"` são o mesmo produto; a unicidade fica num índice simples em `sku`.
- O SKU não muda depois da criação: `PUT` substitui só `nome` e `preco`; estoque muda só por `PATCH .../estoque`.
- O `UPDATE` do `PUT` grava **apenas** `nome` e `preco` (`UPDATE produtos SET nome = @Nome, preco = @Preco WHERE id = @Id`).
  Gravar também o estoque lido antes desfaria um ajuste concorrente feito entre a leitura e a gravação.
- Alternativa descartada: guardar como digitado com índice em `lower(sku)` — mantém grafias diferentes do mesmo código.

## Modelo de dados (script `0003_criar_produtos.sql`)

| Coluna | Tipo |
|--------|------|
| `id` | `integer generated always as identity primary key` |
| `sku` | `varchar(50) not null` |
| `nome` | `varchar(150) not null` |
| `preco` | `numeric(12,2) not null check (preco >= 0)` |
| `estoque` | `int not null check (estoque between 0 and 1000000)` |
| `criado_em` | `timestamptz not null` |

Índice: `create unique index ux_produtos_sku on produtos (sku)`.

## Risks / Trade-offs

- [Limite de 1.000.000 unidades é arbitrário] → constante única na entidade, refletida no `check`; mudar exige nova migração.
- [SKU só com `A-Z 0-9 - _ .`] → códigos com acentos ou espaços internos são recusados com `400` e mensagem clara.
- [Consulta extra quando o ajuste não afeta linhas] → só no caminho de erro; o caminho feliz é uma instrução.
