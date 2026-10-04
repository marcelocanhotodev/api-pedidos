## ADDED Requirements

### Requirement: Versionamento da API
O sistema DEVE (SHALL) expor todos os endpoints de negócio agrupados sob o prefixo `/api/v1`.

#### Scenario: Rota versionada
- **WHEN** um cliente chama `POST /api/v1/auth/token`
- **THEN** a API responde pelo endpoint de emissão de token

#### Scenario: Rota sem versão
- **WHEN** um cliente chama `POST /auth/token`
- **THEN** a API responde `404 Not Found`

## MODIFIED Requirements

### Requirement: Documentação da API
O sistema DEVE (SHALL) publicar o documento OpenAPI e a interface Swagger em `/swagger`, descrevendo endpoints, modelos, códigos de status e o esquema de segurança JWT Bearer.

#### Scenario: Swagger disponível
- **WHEN** um cliente abre `/swagger` com o container em execução
- **THEN** a interface lista todos os endpoints `/api/v1` e oferece o botão Authorize

#### Scenario: Endpoint protegido documentado
- **WHEN** o documento OpenAPI é gerado
- **THEN** os endpoints protegidos declaram o requisito de segurança Bearer e `POST /api/v1/auth/token` não o declara
