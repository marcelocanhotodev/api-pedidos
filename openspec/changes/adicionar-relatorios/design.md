## Context

Escritas passam por repositórios e entidades do Domain. Um relatório agregado não se encaixa nesse modelo:
montar entidades `Pedido` para somar totais seria lento e sem propósito. Esta fatia introduz o lado de leitura.

## Goals / Non-Goals

**Goals:**
- Ranking calculado inteiramente no PostgreSQL, em uma única consulta parametrizada.

**Non-Goals:**
- Read-model materializado, cache, exportação (CSV/PDF), outros relatórios.

## Decisions

- **CQRS leve.** `IRelatorioQueries` é declarada na Application (não no Domain, pois não é repositório de agregado) e
  devolve um DTO de leitura; a implementação Dapper fica em `Infrastructure/Dados/Consultas`.
  Alternativa descartada: método de relatório em `IPedidoRepository` — mistura leitura analítica com o agregado.
- **Forma da consulta.** CTE `qualificados` (pedidos `Pago`/`Enviado` em `[de, ate]`) → CTE `agregados` por cliente
  (`count`, `sum(total)`, `min(criado_em)`) → `ROW_NUMBER() OVER (PARTITION BY cliente_id ORDER BY criado_em DESC)`
  para o último pedido → posição com `ROW_NUMBER() OVER (ORDER BY total_gasto DESC, primeiro_pedido_em ASC)`,
  que garante posições consecutivas no desempate exigido pela spec. `LIMIT @top`.
- **`ticketMedio`** calculado no SQL (`round(total_gasto / quantidade_pedidos, 2)`).
- **Índices.** Reaproveita `pedidos(status)` e `pedidos(cliente_id, criado_em desc)` criados em `0004`; a spec desta
  fatia só verifica que existem.

## Risks / Trade-offs

- [SQL específico do PostgreSQL] → isolado em uma única classe; documentado como ponto de revisão ao trocar de provedor.
- [Volume grande de pedidos] → filtro por período obrigatório e índice por status; `top` limitado a 100.
