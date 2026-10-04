# seguranca Specification

## Purpose

Garante que somente clientes autenticados acessem a API, emitindo tokens JWT para o usuário configurado e protegendo todos os endpoints de negócio.

## Requirements

### Requirement: Emissão de Token
O sistema DEVE (SHALL) oferecer `POST /api/v1/auth/token`, implementado pelo caso de uso `GerarTokenUseCase`, que recebe `usuario` e `senha` e devolve um JWT (`accessToken`) e `expiraEm`, a validade do token em segundos a partir da emissão, quando as credenciais coincidem com o usuário configurado.

#### Scenario: Credenciais válidas
- **WHEN** o cliente envia `AUTH_USUARIO` e `AUTH_SENHA` corretos
- **THEN** a API responde `200` com um JWT que expira em `JWT_EXPIRA_MINUTOS` e `expiraEm` igual a `JWT_EXPIRA_MINUTOS × 60`

#### Scenario: Credenciais inválidas
- **WHEN** o cliente envia credenciais incorretas
- **THEN** a API responde `401 Unauthorized` como `ProblemDetails`

#### Scenario: Campos ausentes
- **WHEN** `usuario` ou `senha` não é informado
- **THEN** a API responde `400` indicando o campo ausente

### Requirement: Proteção dos Endpoints
O sistema DEVE (SHALL) exigir token JWT Bearer válido em todos os endpoints `/api/v1`, exceto `POST /api/v1/auth/token`, sem que cada endpoint precise declarar a proteção. `GET /info`, `/health/*` e `/swagger` permanecem sem autenticação.

#### Scenario: Sem token
- **WHEN** um cliente chama um endpoint protegido de `/api/v1` sem token
- **THEN** a API responde `401 Unauthorized` como `application/problem+json`

#### Scenario: Token expirado ou adulterado
- **WHEN** o cliente envia token expirado (sem tolerância de relógio) ou com assinatura inválida
- **THEN** a API responde `401 Unauthorized`

#### Scenario: Token válido
- **WHEN** o cliente envia um token emitido por `/api/v1/auth/token` e ainda válido
- **THEN** a requisição ao endpoint protegido é processada

#### Scenario: Endpoint anônimo indevido
- **WHEN** um teste sobe a aplicação e inspeciona as rotas registradas sob `/api/v1`
- **THEN** falha se alguma rota além de `POST /api/v1/auth/token` permitir acesso anônimo

#### Scenario: Rotas públicas continuam abertas
- **WHEN** um cliente chama `GET /info` ou `/health/ready` sem token
- **THEN** a API responde normalmente, sem `401`

### Requirement: Tratamento de Segredos
O sistema DEVE (SHALL) ler a chave de assinatura do JWT (mínimo de 32 caracteres), emissor, audiência, validade e credenciais a partir das variáveis de ambiente `JWT_CHAVE`, `JWT_EMISSOR`, `JWT_AUDIENCIA`, `JWT_EXPIRA_MINUTOS`, `AUTH_USUARIO` e `AUTH_SENHA`, documentadas em `.env.example` com valores apenas de desenvolvimento, e DEVE (SHALL) recusar a inicialização se a chave estiver ausente ou curta.

#### Scenario: Chave ausente
- **WHEN** a aplicação inicia sem `JWT_CHAVE`
- **THEN** o processo encerra com mensagem clara e código de saída diferente de zero

#### Scenario: Chave curta
- **WHEN** a aplicação inicia com `JWT_CHAVE` de menos de 32 caracteres
- **THEN** o processo encerra com mensagem clara e código de saída diferente de zero
