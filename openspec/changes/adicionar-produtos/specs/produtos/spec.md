## Purpose

Mantém o catálogo de produtos vendidos, com SKU único, preço e estoque disponível para os pedidos.

## ADDED Requirements

### Requirement: Casos de Uso de Produtos
O recurso DEVE (SHALL) ser implementado pelos casos de uso abaixo, um por endpoint:

| Endpoint | Caso de uso |
|----------|-------------|
| `POST /api/v1/produtos` | `CriarProdutoUseCase` |
| `GET /api/v1/produtos/{id}` | `ObterProdutoUseCase` |
| `GET /api/v1/produtos` | `ListarProdutosUseCase` |
| `PUT /api/v1/produtos/{id}` | `AtualizarProdutoUseCase` |
| `PATCH /api/v1/produtos/{id}/estoque` | `AjustarEstoqueProdutoUseCase` |

#### Scenario: Casos de uso presentes
- **WHEN** a Application é inspecionada
- **THEN** existem as cinco classes da tabela em `CasosDeUso/Produtos/`, cada uma com `UseCaseTests` correspondente

### Requirement: Criar Produto
O sistema DEVE (SHALL) criar um produto a partir de `sku` (único, até 50 caracteres), `nome` (1–150 caracteres), `preco` (>= 0, duas casas decimais) e `estoque` inicial (>= 0).

#### Scenario: Criação com sucesso
- **WHEN** dados válidos são enviados
- **THEN** a API responde `201` com o produto criado

#### Scenario: SKU duplicado
- **WHEN** o SKU já existe
- **THEN** a API responde `409 Conflict`

#### Scenario: Preço negativo
- **WHEN** o preço enviado é negativo
- **THEN** a API responde `400` indicando o campo `preco`

### Requirement: Consultar e Atualizar Produtos
O sistema DEVE (SHALL) retornar um produto por id, listar produtos paginados com filtro opcional `busca` (nome ou SKU) e atualizar `nome` e `preco` por `PUT`.

#### Scenario: Obter por id
- **WHEN** o produto solicitado existe
- **THEN** a API responde `200` com o produto, incluindo `estoque`

#### Scenario: Busca por SKU
- **WHEN** a listagem é chamada com `busca` igual a parte de um SKU
- **THEN** retornam os produtos cujo nome ou SKU contém o termo, no envelope padrão de paginação

#### Scenario: Atualização de preço
- **WHEN** um novo `preco` válido é enviado por `PUT`
- **THEN** a API responde `200` com o preço atualizado e o estoque inalterado

### Requirement: Ajustar Estoque
O sistema DEVE (SHALL) ajustar o estoque por `PATCH /api/v1/produtos/{id}/estoque` com um `delta` (positivo ou negativo) e DEVE (SHALL) rejeitar ajustes que deixariam o estoque negativo.

#### Scenario: Aumentar estoque
- **WHEN** o cliente envia `delta = 10` para um produto com estoque 5
- **THEN** o estoque passa a 15

#### Scenario: Resultado negativo
- **WHEN** o cliente envia `delta = -10` para um produto com estoque 5
- **THEN** a API responde `422` e o estoque permanece 5

### Requirement: Integridade de Produtos no Banco
O esquema DEVE (SHALL) garantir índice único em `sku` e restrições `check` de `preco >= 0` e `estoque >= 0` na tabela `produtos`.

#### Scenario: Estoque negativo bloqueado
- **WHEN** um `UPDATE` tentaria deixar o estoque negativo
- **THEN** o banco rejeita a operação pela restrição `check`
