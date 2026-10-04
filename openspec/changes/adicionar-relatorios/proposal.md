# Change: Adicionar Relatórios

## Why
Com pedidos pagos e enviados no banco, falta uma visão analítica de quem são os melhores clientes. O ranking também
demonstra o lado de leitura do CQRS leve: SQL explícito com CTE e funções de janela, sem passar por repositórios.

## What Changes
- `GET /api/v1/relatorios/ranking-clientes` com período obrigatório e `top`.
- Caso de uso `ObterRankingClientesUseCase` sobre a abstração de leitura `IRelatorioQueries`.
- Consulta Dapper com CTE + `RANK`/`ROW_NUMBER` em `Infrastructure/Dados/Consultas`.
- Nenhuma migração nova: os índices necessários já são criados em `adicionar-pedidos`.

Depende de: `adicionar-pedidos`.

## Capabilities

### New Capabilities
- `relatorios`: ranking de clientes por total gasto no período

### Modified Capabilities
_Nenhuma._

## Impact
- Código: `Application/Abstracoes/IRelatorioQueries`, `CasosDeUso/Relatorios/ObterRankingClientes/`,
  `Infrastructure/Dados/Consultas/RelatorioQueries`, `Api/Endpoints/Relatorios/`.
- Banco: somente leitura.
