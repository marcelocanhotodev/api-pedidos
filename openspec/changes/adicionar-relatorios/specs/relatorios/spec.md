## Purpose

Fornece visões analíticas de leitura sobre os pedidos, começando pelo ranking dos clientes que mais gastaram em um período.

## ADDED Requirements

### Requirement: Casos de Uso de Relatórios
O recurso DEVE (SHALL) ser implementado pelo caso de uso `ObterRankingClientesUseCase`, exposto por `GET /api/v1/relatorios/ranking-clientes`.

#### Scenario: Caso de uso presente
- **WHEN** a Application é inspecionada
- **THEN** existe `ObterRankingClientesUseCase` em `CasosDeUso/Relatorios/`, com `ObterRankingClientesUseCaseTests` correspondente

### Requirement: Relatório de Ranking de Clientes
O sistema DEVE (SHALL) oferecer `GET /api/v1/relatorios/ranking-clientes` com os parâmetros `de`, `ate` (obrigatórios, datas UTC) e `top` (padrão 10, máximo 100), retornando os clientes ordenados pelo total gasto no período e considerando apenas pedidos `Pago` ou `Enviado`. Cada linha DEVE (SHALL) conter `posicao`, `clienteId`, `clienteNome`, `quantidadePedidos`, `totalGasto`, `ticketMedio` e o pedido qualificado mais recente do cliente (`ultimoPedidoId`, `ultimoPedidoEm`, `ultimoPedidoTotal`).

#### Scenario: Ordem do ranking
- **WHEN** o cliente A gastou 900,00 e o cliente B gastou 1200,00 no período
- **THEN** B tem `posicao` 1 e A tem `posicao` 2

#### Scenario: Pedidos pendentes e cancelados ignorados
- **WHEN** um cliente só tem pedidos `Pendente` ou `Cancelado` no período
- **THEN** ele não aparece no relatório

#### Scenario: Empate no total
- **WHEN** dois clientes têm o mesmo `totalGasto`
- **THEN** o cliente com o primeiro pedido mais antigo no período fica à frente, e as posições são consecutivas, sem lacunas

#### Scenario: Limite de linhas
- **WHEN** existem 15 clientes qualificados e `top` não é informado
- **THEN** retornam 10 linhas

#### Scenario: Parâmetros inválidos
- **WHEN** `de` ou `ate` está ausente, `de > ate` ou `top` é maior que 100
- **THEN** a API responde `400`

### Requirement: Implementação com Dapper
A consulta do relatório DEVE (SHALL) ser SQL parametrizado executado com Dapper, usando CTE e funções de janela (`RANK`/`ROW_NUMBER`), residir na camada Infrastructure atrás da interface `IRelatorioQueries` (declarada na Application) e NÃO DEVE (SHALL NOT) concatenar parâmetros no texto SQL.

#### Scenario: Tentativa de injeção
- **WHEN** um parâmetro contém fragmentos de SQL
- **THEN** ele é tratado como dado e a consulta se comporta como para um valor inválido

### Requirement: Suporte de Índices ao Relatório
O esquema DEVE (SHALL) incluir os índices `pedidos(status)` e `pedidos(cliente_id, criado_em desc)` para suportar o filtro do relatório.

#### Scenario: Índices presentes
- **WHEN** as migrações são aplicadas
- **THEN** ambos os índices existem na tabela `pedidos`
