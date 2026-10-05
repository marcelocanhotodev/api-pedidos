# Change: Adicionar Pedidos

## Why
Pedidos são o núcleo do domínio: concentram as regras de desconto, o ciclo de status, a reserva de estoque e o
controle de concorrência. Com clientes e produtos já entregues, esta fatia trata só dessas regras.

## What Changes
- Criação de pedido com itens, reserva atômica de estoque e regra de desconto.
- Ciclo de status `Pendente → Pago → Enviado`, com cancelamento que devolve o estoque.
- Concorrência otimista por `versao` no pedido.
- Consulta por id e listagem paginada com filtros.
- Casos de uso `CriarPedidoUseCase`, `ObterPedidoUseCase`, `ListarPedidosUseCase`, `PagarPedidoUseCase`, `EnviarPedidoUseCase`, `CancelarPedidoUseCase`.
- Migração `0004_criar_pedidos.sql` (pedidos, itens e índices), com ids `integer` autoincrementais e chaves estrangeiras inteiras.
- Exclusão de cliente passa a ser bloqueada quando ele possui pedidos.

Depende de: `adicionar-clientes`, `usar-ids-inteiros` e `adicionar-produtos` (todas arquivadas antes desta).

## Capabilities

### New Capabilities
- `pedidos`: criação, reserva de estoque, desconto, transições de status, concorrência e consulta de pedidos

### Modified Capabilities
- `clientes`: "Excluir Cliente" passa a responder `409` quando o cliente possui pedidos
- `qualidade`: adiciona requisito de cobertura específico dos casos de uso de pedidos

## Impact
- Código: `Domain/Entidades/{Pedido,ItemPedido}`, `Enums/StatusPedido`, `Regras/` (desconto), `CasosDeUso/Pedidos/*`,
  `PedidoRepository`, `Api/Endpoints/Pedidos/*`; `ExcluirClienteUseCase` ganha dependência de `IPedidoRepository`.
- Banco: tabelas `pedidos` e `itens_pedido` com chaves estrangeiras para `clientes` e `produtos` (script `0004`).
