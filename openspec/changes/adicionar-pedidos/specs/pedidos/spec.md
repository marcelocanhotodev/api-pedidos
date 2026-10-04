## Purpose

Permite que clientes façam pedidos de produtos, aplicando desconto, reservando estoque e controlando o ciclo de vida do pedido de forma segura sob concorrência.

## ADDED Requirements

### Requirement: Casos de Uso de Pedidos
O recurso DEVE (SHALL) ser implementado pelos casos de uso abaixo, um por endpoint:

| Endpoint | Caso de uso |
|----------|-------------|
| `POST /api/v1/pedidos` | `CriarPedidoUseCase` |
| `GET /api/v1/pedidos/{id}` | `ObterPedidoUseCase` |
| `GET /api/v1/pedidos` | `ListarPedidosUseCase` |
| `POST /api/v1/pedidos/{id}/pagar` | `PagarPedidoUseCase` |
| `POST /api/v1/pedidos/{id}/enviar` | `EnviarPedidoUseCase` |
| `POST /api/v1/pedidos/{id}/cancelar` | `CancelarPedidoUseCase` |

#### Scenario: Casos de uso presentes
- **WHEN** a Application é inspecionada
- **THEN** existem as seis classes da tabela em `CasosDeUso/Pedidos/`, cada uma com `UseCaseTests` correspondente

### Requirement: Criar Pedido
O sistema DEVE (SHALL) criar um pedido para um cliente existente com um ou mais itens (`produtoId`, `quantidade` >= 1), gravar em cada item o `precoUnitario` vigente do produto no momento da criação, definir o status `Pendente` e responder `201 Created`.

#### Scenario: Pedido válido
- **WHEN** o cliente envia um pedido com cliente e produtos válidos e com estoque
- **THEN** a API responde `201`, com status `Pendente` e `subtotal`, `desconto` e `total` calculados

#### Scenario: Cliente ou produto inexistente
- **WHEN** o cliente ou algum produto não existe
- **THEN** a API responde `404`

#### Scenario: Itens vazios ou produto repetido
- **WHEN** o pedido não tem itens ou repete o mesmo produto
- **THEN** a API responde `400`

#### Scenario: Alterar preço preserva pedidos antigos
- **WHEN** o preço de um produto muda depois de pedidos criados
- **THEN** os itens dos pedidos existentes mantêm o `precoUnitario` original

### Requirement: Reserva de Estoque
O sistema DEVE (SHALL) baixar o estoque de cada item de forma atômica, na mesma transação que cria o pedido, e DEVE (SHALL) rejeitar o pedido inteiro se qualquer item não tiver estoque suficiente.

#### Scenario: Estoque insuficiente
- **WHEN** um item pede 5 unidades e há apenas 3
- **THEN** a API responde `422`, nenhum pedido é criado e nenhum estoque é alterado

#### Scenario: Falha no segundo item
- **WHEN** o primeiro item tem estoque e o segundo não
- **THEN** a transação sofre rollback e o estoque do primeiro produto permanece inalterado

#### Scenario: Última unidade disputada
- **WHEN** duas requisições simultâneas tentam comprar a última unidade
- **THEN** exatamente uma tem sucesso e a outra recebe `422`

### Requirement: Regra de Desconto
O sistema DEVE (SHALL) calcular `subtotal` como a soma de `quantidade × precoUnitario` e aplicar desconto de 10% quando `subtotal >= 1000,00`, de 5% quando `subtotal >= 500,00` e menor que 1000,00, e 0% nos demais casos; `total` DEVE (SHALL) ser `subtotal - desconto`, arredondado para duas casas (afastando de zero).

#### Scenario: Faixa de 10%
- **WHEN** o subtotal é 1000,00
- **THEN** o desconto é 100,00 e o total é 900,00

#### Scenario: Faixa de 5%
- **WHEN** o subtotal é 500,00
- **THEN** o desconto é 25,00 e o total é 475,00

#### Scenario: Limite superior da faixa de 5%
- **WHEN** o subtotal é 999,99
- **THEN** o desconto é 50,00 e o total é 949,99

#### Scenario: Sem desconto
- **WHEN** o subtotal é 499,99
- **THEN** o desconto é 0,00 e o total é 499,99

### Requirement: Transições de Status
O sistema DEVE (SHALL) permitir somente as transições `Pendente → Pago` (`/pagar`), `Pago → Enviado` (`/enviar`) e `Pendente|Pago → Cancelado` (`/cancelar`); qualquer outra transição DEVE (SHALL) ser rejeitada com `422`.

#### Scenario: Pagar pedido pendente
- **WHEN** `POST /pedidos/{id}/pagar` é chamado para um pedido `Pendente`
- **THEN** o status passa a `Pago`

#### Scenario: Enviar pedido não pago
- **WHEN** `/enviar` é chamado para um pedido `Pendente`
- **THEN** a API responde `422` e o status permanece `Pendente`

#### Scenario: Cancelar pedido enviado
- **WHEN** `/cancelar` é chamado para um pedido `Enviado`
- **THEN** a API responde `422`

### Requirement: Cancelamento Devolve Estoque
O sistema DEVE (SHALL) devolver ao estoque as quantidades reservadas quando um pedido for cancelado, na mesma transação da mudança de status.

#### Scenario: Estoque devolvido
- **WHEN** um pedido com 2 unidades do produto X é cancelado
- **THEN** o estoque do produto X aumenta em 2

### Requirement: Controle de Concorrência
O sistema DEVE (SHALL) detectar modificações concorrentes do mesmo pedido por concorrência otimista (coluna `versao`) e responder `409 Conflict` à requisição perdedora.

#### Scenario: Pagamento duplo
- **WHEN** dois clientes chamam `/pagar` no mesmo pedido ao mesmo tempo
- **THEN** um tem sucesso e o outro recebe `409` ou `422`

### Requirement: Consultar Pedidos
O sistema DEVE (SHALL) retornar um pedido com seus itens por id e uma listagem paginada filtrável por `clienteId`, `status`, `de` e `ate` (data de criação, UTC, inclusiva), ordenada por `criadoEm` decrescente.

#### Scenario: Obter com itens
- **WHEN** um pedido existente é consultado por id
- **THEN** a API responde `200` com o pedido e seus itens, incluindo `precoUnitario`

#### Scenario: Filtro por status e cliente
- **WHEN** a listagem é chamada com `status=Pago&clienteId={id}`
- **THEN** retornam apenas pedidos pagos daquele cliente, do mais novo para o mais antigo

#### Scenario: Intervalo inválido
- **WHEN** `de` é posterior a `ate`
- **THEN** a API responde `400`

### Requirement: Integridade de Pedidos no Banco
O esquema DEVE (SHALL) garantir chaves estrangeiras de `pedidos` para `clientes` e de `itens_pedido` para `pedidos` e `produtos`, a restrição `check (quantidade > 0)` em `itens_pedido` e os índices `pedidos(cliente_id, criado_em desc)`, `pedidos(status)`, `itens_pedido(pedido_id)` e `itens_pedido(produto_id)`.

#### Scenario: Item com produto inexistente bloqueado
- **WHEN** um `INSERT` em `itens_pedido` referencia um `produto_id` inexistente
- **THEN** o banco rejeita a operação pela chave estrangeira

#### Scenario: Índices presentes
- **WHEN** as migrações são aplicadas
- **THEN** os quatro índices existem
