## MODIFIED Requirements

### Requirement: Emissão de Token
O sistema DEVE (SHALL) oferecer `POST /auth/token`, fora de qualquer prefixo de versão, implementado pelo caso de uso `GerarTokenUseCase`, que recebe `usuario` e `senha` e devolve um JWT (`accessToken`) e `expiraEm`, a validade do token em segundos a partir da emissão, quando as credenciais coincidem com o usuário configurado.

#### Scenario: Credenciais válidas
- **WHEN** o cliente envia `AUTH_USUARIO` e `AUTH_SENHA` corretos para `POST /auth/token`
- **THEN** a API responde `200` com um JWT que expira em `JWT_EXPIRA_MINUTOS` e `expiraEm` igual a `JWT_EXPIRA_MINUTOS × 60`

#### Scenario: Credenciais inválidas
- **WHEN** o cliente envia credenciais incorretas
- **THEN** a API responde `401 Unauthorized` como `ProblemDetails`

#### Scenario: Campos ausentes
- **WHEN** `usuario` ou `senha` não é informado
- **THEN** a API responde `400` indicando o campo ausente

#### Scenario: Rota versionada removida
- **WHEN** um cliente chama `POST /api/v1/auth/token`
- **THEN** a API responde `404 Not Found`

### Requirement: Proteção dos Endpoints
O sistema DEVE (SHALL) exigir token JWT Bearer válido em todos os endpoints `/api/v1`, sem exceção e sem que cada endpoint precise declarar a proteção. `POST /auth/token`, `GET /info`, `/health/*` e `/swagger` ficam fora de `/api/v1` e permanecem sem autenticação.

#### Scenario: Sem token
- **WHEN** um cliente chama um endpoint protegido de `/api/v1` sem token
- **THEN** a API responde `401 Unauthorized` como `application/problem+json`

#### Scenario: Token expirado ou adulterado
- **WHEN** o cliente envia token expirado (sem tolerância de relógio) ou com assinatura inválida
- **THEN** a API responde `401 Unauthorized`

#### Scenario: Token válido
- **WHEN** o cliente envia um token emitido por `POST /auth/token` e ainda válido
- **THEN** a requisição ao endpoint protegido é processada

#### Scenario: Endpoint anônimo indevido
- **WHEN** um teste sobe a aplicação e inspeciona as rotas registradas sob `/api/v1`
- **THEN** falha se qualquer rota sob `/api/v1` permitir acesso anônimo

#### Scenario: Rotas públicas continuam abertas
- **WHEN** um cliente chama `POST /auth/token`, `GET /info` ou `/health/ready` sem token
- **THEN** a API responde normalmente, sem `401`
