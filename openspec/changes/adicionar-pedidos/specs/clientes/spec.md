## MODIFIED Requirements

### Requirement: Excluir Cliente
O sistema DEVE (SHALL) excluir um cliente existente por `DELETE /api/v1/clientes/{id}` somente quando ele não possuir pedidos.

#### Scenario: Exclusão com sucesso
- **WHEN** um cliente existente e sem pedidos é excluído
- **THEN** a API responde `204 No Content` e o cliente deixa de ser retornado

#### Scenario: Cliente inexistente
- **WHEN** o id não corresponde a nenhum cliente
- **THEN** a API responde `404`

#### Scenario: Com pedidos
- **WHEN** um cliente com pedidos é excluído
- **THEN** a API responde `409 Conflict` e o cliente é mantido
