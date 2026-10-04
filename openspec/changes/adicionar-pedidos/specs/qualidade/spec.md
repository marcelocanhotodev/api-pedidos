## ADDED Requirements

### Requirement: Cobertura dos Casos de Uso de Pedidos
Os testes unitários dos casos de uso de pedidos DEVEM (SHALL) cobrir os limites do desconto, o controle transacional e todas as transições de status, e os testes de integração DEVEM (SHALL) cobrir o fluxo completo do pedido contra PostgreSQL real.

#### Scenario: Limites do desconto
- **WHEN** `CriarPedidoUseCaseTests` executa
- **THEN** os subtotais 499,99, 500,00, 999,99 e 1000,00 são verificados com o desconto esperado

#### Scenario: Transações verificadas
- **WHEN** `CriarPedidoUseCaseTests` simula falta de estoque no segundo item
- **THEN** o teste verifica que `IUnitOfWork` executou rollback e nunca commit

#### Scenario: Transições de status
- **WHEN** `PagarPedidoUseCaseTests`, `EnviarPedidoUseCaseTests` e `CancelarPedidoUseCaseTests` executam
- **THEN** transições válidas e inválidas são cobertas, e o cancelamento verifica a devolução de estoque

#### Scenario: Fluxo de pedido ponta a ponta
- **WHEN** um teste de integração cria cliente e produto, cria o pedido, paga e envia
- **THEN** respostas, status e estoque coincidem com a especificação de pedidos
