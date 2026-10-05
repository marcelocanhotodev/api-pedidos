## MODIFIED Requirements

### Requirement: Documentação da API
O sistema DEVE (SHALL) publicar o documento OpenAPI e a interface Swagger em `/swagger`, descrevendo endpoints, modelos, códigos de status e o esquema de segurança JWT Bearer.

#### Scenario: Swagger disponível
- **WHEN** um cliente abre `/swagger` com o container em execução
- **THEN** a interface lista todos os endpoints `/api/v1`, `POST /auth/token` e `GET /info`, e oferece o botão Authorize

#### Scenario: Endpoint protegido documentado
- **WHEN** o documento OpenAPI é gerado
- **THEN** os endpoints protegidos declaram o requisito de segurança Bearer e `POST /auth/token` não o declara

### Requirement: Versionamento da API
O sistema DEVE (SHALL) expor todos os endpoints de negócio agrupados sob o prefixo `/api/v1`. Endpoints de infraestrutura de acesso e operação — `POST /auth/token`, `GET /info` e `/health/*` — NÃO DEVEM (SHALL NOT) ser versionados, para que não mudem quando surgirem novas versões do contrato de negócio.

#### Scenario: Rota versionada
- **WHEN** um cliente autenticado chama `GET /api/v1/clientes`
- **THEN** a API responde pelo endpoint de listagem de clientes

#### Scenario: Rota sem versão
- **WHEN** um cliente chama `GET /clientes`
- **THEN** a API responde `404 Not Found`

#### Scenario: Autenticação sem versão
- **WHEN** um cliente chama `POST /auth/token`
- **THEN** a API responde pelo endpoint de emissão de token, fora de `/api/v1`
