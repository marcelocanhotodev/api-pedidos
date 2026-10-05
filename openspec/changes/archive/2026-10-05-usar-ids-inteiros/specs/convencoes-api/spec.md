## ADDED Requirements

### Requirement: Identificadores Numéricos
O sistema DEVE (SHALL) expor o `id` de todo recurso como número inteiro no JSON e nas rotas (`/api/v1/<recurso>/{id}`), o mesmo `id` da chave primária no banco, e DEVE (SHALL) responder `400 Bad Request` como `ProblemDetails` quando o `id` da rota não for um inteiro válido.

#### Scenario: Id numérico no JSON
- **WHEN** um recurso é criado ou consultado
- **THEN** o campo `id` da resposta é um número inteiro, e o cabeçalho `Location` usa esse mesmo número

#### Scenario: Id não numérico na rota
- **WHEN** um cliente chama `GET /api/v1/clientes/abc`
- **THEN** a API responde `400` como `ProblemDetails` indicando o parâmetro `id`

#### Scenario: Id numérico inexistente
- **WHEN** um cliente chama `GET /api/v1/clientes/999999` e esse id não existe
- **THEN** a API responde `404`
