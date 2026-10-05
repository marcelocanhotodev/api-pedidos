# produtos Specification

## Purpose

Mantém o catálogo de produtos vendidos, com SKU único, preço e estoque disponível para os pedidos.

## Requirements

### Requirement: Casos de Uso de Produtos
O recurso DEVE (SHALL) ser implementado pelos casos de uso abaixo, um por endpoint, todos sob `/api/v1` e protegidos por JWT:

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

#### Scenario: Acesso sem token
- **WHEN** qualquer endpoint de produtos é chamado sem token
- **THEN** a API responde `401 Unauthorized`

### Requirement: Criar Produto
O sistema DEVE (SHALL) criar um produto a partir de `sku`, `nome`, `preco` e `estoque` inicial, gerar `criadoEm`, obter o `id` inteiro gerado pelo banco e responder `201 Created` com o cabeçalho `Location`. O `sku` DEVE (SHALL) ser gravado sem espaços nas pontas e em maiúsculas, ter de 1 a 50 caracteres entre `A-Z`, `0-9`, `-`, `_` e `.`, e ser único. O `nome` DEVE (SHALL) ter de 1 a 150 caracteres após remover espaços nas pontas. O `preco` DEVE (SHALL) estar entre `0` e `9999999999.99`, com no máximo duas casas decimais, e DEVE (SHALL) ser sempre devolvido com exatamente duas casas; o `estoque` inicial DEVE (SHALL) estar entre `0` e `1000000`.

#### Scenario: Criação com sucesso
- **WHEN** dados válidos são enviados
- **THEN** a API responde `201` com o produto criado e `Location` apontando para `/api/v1/produtos/{id}`

#### Scenario: SKU normalizado
- **WHEN** o cliente envia `sku = " abc-1 "` e `nome = "  Caneta  "`
- **THEN** o produto é gravado e devolvido com `sku = "ABC-1"` e `nome = "Caneta"`

#### Scenario: SKU duplicado
- **WHEN** o SKU já existe, mesmo enviado com outra caixa ou com espaços nas pontas
- **THEN** a API responde `409 Conflict`

#### Scenario: SKU com caractere inválido
- **WHEN** o SKU contém espaço interno, acento ou símbolo fora de `- _ .`
- **THEN** a API responde `400` indicando o campo `sku`

#### Scenario: Preço negativo
- **WHEN** o preço enviado é negativo
- **THEN** a API responde `400` indicando o campo `preco`

#### Scenario: Preço sempre com duas casas
- **WHEN** o preço enviado é `4.9`
- **THEN** a resposta do `POST` e a de um `GET` posterior trazem `preco` igual a `4.90`

#### Scenario: Preço com mais de duas casas
- **WHEN** o preço enviado é `10.999`
- **THEN** a API responde `400` indicando o campo `preco`, sem arredondar

#### Scenario: Valores acima do limite
- **WHEN** o preço enviado é maior que `9999999999.99` ou o estoque inicial é maior que `1000000`
- **THEN** a API responde `400` indicando o campo, sem erro interno

### Requirement: Consultar e Atualizar Produtos
O sistema DEVE (SHALL) retornar um produto por id, listar produtos paginados com filtro opcional `busca` (nome ou SKU, sem diferenciar maiúsculas, tratando `%`, `_` e `\` como caracteres literais) ordenados por `nome` e, em caso de empate, por `id`, e atualizar por `PUT` apenas `nome` e `preco`, com as mesmas regras da criação. O `sku` NÃO DEVE (SHALL NOT) mudar depois da criação e o `estoque` só muda pelo ajuste de estoque.

#### Scenario: Obter por id
- **WHEN** o produto solicitado existe
- **THEN** a API responde `200` com o produto, incluindo `estoque`

#### Scenario: Id inexistente
- **WHEN** o produto solicitado não existe
- **THEN** a API responde `404` como `ProblemDetails`

#### Scenario: Busca por SKU
- **WHEN** a listagem é chamada com `busca` igual a parte de um SKU, em qualquer caixa
- **THEN** retornam os produtos cujo nome ou SKU contém o termo, no envelope padrão de paginação

#### Scenario: Busca com curinga
- **WHEN** a listagem é chamada com `busca=%`
- **THEN** retornam somente produtos cujo nome ou SKU contém o caractere `%`

#### Scenario: Nomes repetidos entre páginas
- **WHEN** existem três produtos com o mesmo nome e a listagem é percorrida com `tamanhoPagina=1`
- **THEN** cada um dos três aparece exatamente uma vez, nas páginas 1, 2 e 3

#### Scenario: Atualização de preço
- **WHEN** um novo `preco` válido é enviado por `PUT`
- **THEN** a API responde `200` com o preço atualizado e o estoque e o SKU inalterados

#### Scenario: SKU e estoque ignorados no PUT
- **WHEN** o corpo do `PUT` inclui `sku` ou `estoque` diferentes dos atuais
- **THEN** a API responde `200` mantendo o SKU e o estoque originais

#### Scenario: Atualizar produto inexistente
- **WHEN** o id não corresponde a nenhum produto
- **THEN** a API responde `404`

### Requirement: Ajustar Estoque
O sistema DEVE (SHALL) ajustar o estoque por `PATCH /api/v1/produtos/{id}/estoque` com um `delta` inteiro diferente de zero, entre `-1000000` e `1000000`, aplicado de forma atômica, e DEVE (SHALL) rejeitar ajustes que deixariam o estoque fora do intervalo `0..1000000`. A resposta DEVE (SHALL) trazer o produto com o estoque resultante.

#### Scenario: Aumentar estoque
- **WHEN** o cliente envia `delta = 10` para um produto com estoque 5
- **THEN** a API responde `200` com o produto e `estoque = 15`

#### Scenario: Resultado negativo
- **WHEN** o cliente envia `delta = -10` para um produto com estoque 5
- **THEN** a API responde `422` e o estoque permanece 5

#### Scenario: Resultado acima do limite
- **WHEN** o cliente envia `delta = 10` para um produto com estoque `999995`
- **THEN** a API responde `422` e o estoque permanece `999995`

#### Scenario: Delta inválido
- **WHEN** o cliente envia `delta = 0` ou `|delta| > 1000000`
- **THEN** a API responde `400` indicando o campo `delta`

#### Scenario: Ajuste em produto inexistente
- **WHEN** o id não corresponde a nenhum produto
- **THEN** a API responde `404`

#### Scenario: Ajustes simultâneos
- **WHEN** dez requisições simultâneas enviam `delta = 1` para um produto com estoque 0
- **THEN** todas respondem `200` e o estoque final é exatamente 10

#### Scenario: Atualização simultânea a ajustes
- **WHEN** um `PUT` de nome e preço é processado ao mesmo tempo que dez ajustes `delta = 1` em um produto com estoque 0
- **THEN** o produto termina com o nome e o preço do `PUT` e estoque exatamente 10

### Requirement: Integridade de Produtos no Banco
O esquema DEVE (SHALL) garantir índice único em `sku` e as restrições `check (preco >= 0)` e `check (estoque between 0 and 1000000)` na tabela `produtos`.

#### Scenario: Estoque negativo bloqueado
- **WHEN** um `UPDATE` tentaria deixar o estoque negativo
- **THEN** o banco rejeita a operação pela restrição `check`

#### Scenario: Duplicidade de SKU bloqueada no banco
- **WHEN** dois `INSERT` simultâneos tentam gravar o mesmo SKU
- **THEN** o banco rejeita o segundo e a API responde `409 Conflict`
