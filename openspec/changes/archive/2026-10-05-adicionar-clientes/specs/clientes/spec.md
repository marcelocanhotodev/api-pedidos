## Purpose

Permite cadastrar, consultar, atualizar e excluir os clientes que fazem pedidos, garantindo e-mail único.

## ADDED Requirements

### Requirement: Casos de Uso de Clientes
O recurso DEVE (SHALL) ser implementado pelos casos de uso abaixo, um por endpoint, todos sob `/api/v1` e protegidos por JWT:

| Endpoint | Caso de uso |
|----------|-------------|
| `POST /api/v1/clientes` | `CriarClienteUseCase` |
| `GET /api/v1/clientes/{id}` | `ObterClienteUseCase` |
| `GET /api/v1/clientes` | `ListarClientesUseCase` |
| `PUT /api/v1/clientes/{id}` | `AtualizarClienteUseCase` |
| `DELETE /api/v1/clientes/{id}` | `ExcluirClienteUseCase` |

#### Scenario: Casos de uso presentes
- **WHEN** a Application é inspecionada
- **THEN** existem as cinco classes da tabela em `CasosDeUso/Clientes/`, cada uma com `UseCaseTests` correspondente

#### Scenario: Acesso sem token
- **WHEN** qualquer endpoint de clientes é chamado sem token
- **THEN** a API responde `401 Unauthorized`

### Requirement: Criar Cliente
O sistema DEVE (SHALL) criar um cliente a partir de `nome` (1–150 caracteres) e `email` (formato válido, até 200 caracteres), removendo espaços nas pontas de ambos, gerar `id` e `criadoEm` e responder `201 Created` com o cabeçalho `Location`.

#### Scenario: Criação com sucesso
- **WHEN** um cliente autenticado envia dados válidos
- **THEN** a API responde `201`, com o cliente criado e `Location` apontando para `/api/v1/clientes/{id}`

#### Scenario: Espaços nas pontas
- **WHEN** o cliente envia `nome = "  Ana  "` e `email = " ana@x.com "`
- **THEN** o cliente é gravado e devolvido com `nome = "Ana"` e `email = "ana@x.com"`

#### Scenario: E-mail duplicado
- **WHEN** o e-mail já existe (sem diferenciar maiúsculas e minúsculas)
- **THEN** a API responde `409 Conflict`

#### Scenario: E-mail inválido
- **WHEN** o e-mail tem formato inválido
- **THEN** a API responde `400` indicando o campo `email`

### Requirement: Consultar Clientes
O sistema DEVE (SHALL) retornar um cliente por id e uma listagem paginada com filtro opcional `busca` (nome ou e-mail, sem diferenciar maiúsculas, tratando `%`, `_` e `\` como caracteres literais), ordenada por `nome` e, em caso de empate, por `id`.

#### Scenario: Obter por id
- **WHEN** o cliente solicitado existe
- **THEN** a API responde `200` com o cliente

#### Scenario: Id inexistente
- **WHEN** o cliente solicitado não existe
- **THEN** a API responde `404` como `ProblemDetails`

#### Scenario: Busca
- **WHEN** a listagem é chamada com `busca=ana`
- **THEN** retornam somente clientes cujo nome ou e-mail contém "ana" em qualquer caixa

#### Scenario: Busca com curinga
- **WHEN** a listagem é chamada com `busca=%`
- **THEN** retornam somente clientes cujo nome ou e-mail contém o caractere `%`

#### Scenario: Listagem paginada
- **WHEN** a listagem é chamada sem parâmetros de paginação
- **THEN** a resposta usa o envelope padrão de paginação, ordenada por `nome`

#### Scenario: Nomes repetidos entre páginas
- **WHEN** existem três clientes com o mesmo nome e a listagem é percorrida com `tamanhoPagina=1`
- **THEN** cada um dos três aparece exatamente uma vez, nas páginas 1, 2 e 3

### Requirement: Atualizar Cliente
O sistema DEVE (SHALL) substituir `nome` e `email` de um cliente existente por `PUT /api/v1/clientes/{id}`, com as mesmas regras de validação e normalização da criação.

#### Scenario: Atualização válida
- **WHEN** dados válidos são enviados para um cliente existente
- **THEN** a API responde `200` com o cliente atualizado

#### Scenario: E-mail de outro cliente
- **WHEN** o novo e-mail pertence a outro cliente
- **THEN** a API responde `409 Conflict`

#### Scenario: Mantém o próprio e-mail
- **WHEN** o cliente é atualizado mantendo o próprio e-mail, com outra caixa
- **THEN** a API responde `200` sem acusar conflito

#### Scenario: Cliente inexistente
- **WHEN** o id não corresponde a nenhum cliente
- **THEN** a API responde `404`

### Requirement: Excluir Cliente
O sistema DEVE (SHALL) excluir um cliente existente por `DELETE /api/v1/clientes/{id}`.

#### Scenario: Exclusão com sucesso
- **WHEN** um cliente existente é excluído
- **THEN** a API responde `204 No Content` e o cliente deixa de ser retornado

#### Scenario: Cliente inexistente
- **WHEN** o id não corresponde a nenhum cliente
- **THEN** a API responde `404`

### Requirement: Integridade de Clientes no Banco
O esquema DEVE (SHALL) garantir a unicidade do e-mail sem diferenciar maiúsculas por índice único em `lower(email)` na tabela `clientes`.

#### Scenario: Duplicidade bloqueada no banco
- **WHEN** dois `INSERT` simultâneos tentam gravar `Ana@x.com` e `ana@x.com`
- **THEN** o banco rejeita o segundo e a API responde `409 Conflict`
