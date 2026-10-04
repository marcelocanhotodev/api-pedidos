## Context

Clientes e produtos já existem, com repositórios Dapper e `IUnitOfWork` por requisição (ver specs arquivadas).
Esta fatia introduz o primeiro agregado com filhos (`Pedido` → `ItemPedido`) e as primeiras operações que
alteram dois agregados na mesma transação (pedido + estoque de produtos).

## Goals / Non-Goals

**Goals:**
- Regras de desconto e de transição de status puras no Domain, testáveis sem mocks.
- Nenhuma venda acima do estoque, mesmo sob concorrência.

**Non-Goals:**
- Pagamento real, frete, edição de itens de um pedido existente, reabertura de pedido cancelado.

## Decisions

- **Regras no Domain.** `Pedido` calcula `subtotal`, `desconto` e `total` (arredondamento `MidpointRounding.AwayFromZero`)
  e expõe `Pagar()`, `Enviar()` e `Cancelar()`, que lançam `RegraDeNegocioException` em transições inválidas.
  Os casos de uso só orquestram.
- **Baixa de estoque atômica.** `UPDATE produtos SET estoque = estoque - @q WHERE id = @id AND estoque >= @q`;
  zero linhas afetadas → `RegraDeNegocioException` (422) e rollback do pedido inteiro.
  Alternativa descartada: `SELECT ... FOR UPDATE` seguido de `UPDATE` — mais idas ao banco e mais tempo de lock,
  sem ganho para este caso.
- **Concorrência otimista no pedido.** Coluna `versao`; `UPDATE pedidos ... WHERE id = @id AND versao = @versao`
  incrementa a versão; zero linhas → `ConflitoException` (409).
  Como a transição é validada antes do `UPDATE`, o perdedor de um pagamento duplo pode receber 409 (versão mudou)
  ou 422 (já está `Pago`), conforme o momento da leitura — os dois são aceitos pela spec.
- **Ordem determinística de baixa.** Itens são processados em ordem de `produto_id` para reduzir risco de deadlock
  entre pedidos com os mesmos produtos.
- **Preço histórico.** `itens_pedido.preco_unitario` é copiado do produto na criação e nunca recalculado.
- **Índices junto com a tabela.** `pedidos(cliente_id, criado_em desc)`, `pedidos(status)`, `itens_pedido(pedido_id)` e
  `itens_pedido(produto_id)` entram no próprio `0003`, pois a listagem filtrada já depende deles;
  `adicionar-relatorios` apenas verifica que existem.
- **Exclusão de cliente.** `ExcluirClienteUseCase` consulta `IPedidoRepository.ExisteParaClienteAsync`; a chave
  estrangeira `pedidos.cliente_id` (sem cascade) garante a regra mesmo sob concorrência, com a violação traduzida em 409.

## Modelo de dados (script `0003_criar_pedidos.sql`)
| Tabela | Colunas |
|--------|---------|
| `pedidos` | `id uuid pk`, `cliente_id uuid fk → clientes`, `status varchar(20) not null`, `subtotal numeric(12,2)`, `desconto numeric(12,2)`, `total numeric(12,2)`, `versao int not null default 1`, `criado_em timestamptz not null` |
| `itens_pedido` | `id uuid pk`, `pedido_id uuid fk → pedidos on delete cascade`, `produto_id uuid fk → produtos`, `quantidade int not null check (quantidade > 0)`, `preco_unitario numeric(12,2) not null` |

## Risks / Trade-offs

- [Deadlock entre transações que baixam os mesmos produtos] → ordem determinística por `produto_id`; teste de concorrência na integração.
- [Testes de concorrência intermitentes] → usar barreira (`Task.WhenAll` com N requisições) e asserir só o invariante (uma vence, estoque nunca negativo).
- [Mudança de comportamento em `DELETE /clientes/{id}`] → documentada como MODIFIED na spec de clientes.
