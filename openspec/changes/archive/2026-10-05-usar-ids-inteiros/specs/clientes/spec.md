## MODIFIED Requirements

### Requirement: Integridade de Clientes no Banco
O esquema DEVE (SHALL) definir o `id` da tabela `clientes` como `integer` autoincremental gerado pelo banco e garantir a unicidade do e-mail sem diferenciar maiúsculas por índice único em `lower(email)`. A troca da chave `uuid` existente por `integer` DEVE (SHALL) preservar todos os clientes já cadastrados.

#### Scenario: Duplicidade bloqueada no banco
- **WHEN** dois `INSERT` simultâneos tentam gravar `Ana@x.com` e `ana@x.com`
- **THEN** o banco rejeita o segundo e a API responde `409 Conflict`

#### Scenario: Clientes existentes preservados
- **WHEN** a migração para id inteiro é aplicada a um banco que já tem clientes
- **THEN** todos continuam cadastrados, com nome, e-mail e `criadoEm` inalterados e um id inteiro único cada

#### Scenario: Unicidade de e-mail mantida após a conversão
- **WHEN** um cliente é criado depois da conversão com o e-mail de um cliente preexistente, em outra caixa
- **THEN** a API responde `409 Conflict`
