## Purpose

Define as convenções HTTP transversais da API: formato padrão de erros, documentação OpenAPI, health checks e logs estruturados.

## ADDED Requirements

### Requirement: Formato Padrão de Erros
O sistema DEVE (SHALL) devolver todo erro como `application/problem+json` (RFC 7807) com `status`, `title`, `detail` e `traceId`, e NÃO DEVE (SHALL NOT) expor stack trace nem mensagens internas. As exceções de domínio DEVEM (SHALL) ser convertidas por um único tratador global: não encontrado → `404`, conflito → `409`, regra de negócio → `422`.

#### Scenario: Recurso não encontrado
- **WHEN** um caso de uso sinaliza que o recurso solicitado não existe
- **THEN** a API responde `404` com corpo `ProblemDetails`

#### Scenario: Falha de validação
- **WHEN** o corpo da requisição não passa na validação
- **THEN** a API responde `400` com `errors` mapeando cada campo inválido às suas mensagens

#### Scenario: Violação de regra de negócio
- **WHEN** um caso de uso sinaliza violação de regra de negócio
- **THEN** a API responde `422 Unprocessable Entity` com `ProblemDetails` descrevendo a regra

#### Scenario: Conflito
- **WHEN** um caso de uso sinaliza conflito com o estado atual
- **THEN** a API responde `409 Conflict`

#### Scenario: Falha inesperada
- **WHEN** ocorre uma exceção não tratada
- **THEN** a API registra o erro com o `traceId` e responde `500` com `ProblemDetails` genérico

### Requirement: Paginação
O sistema DEVE (SHALL) paginar toda listagem com os parâmetros `pagina` (padrão 1, mínimo 1) e `tamanhoPagina` (padrão 20, mínimo 1, máximo 100) e retornar um envelope com `itens`, `pagina`, `tamanhoPagina`, `totalItens` e `totalPaginas`, usando um modelo de paginação e uma regra de validação compartilhados por todas as listagens.

#### Scenario: Paginação padrão
- **WHEN** um cliente lista uma coleção sem parâmetros de paginação
- **THEN** a resposta contém no máximo 20 itens, `pagina` igual a 1 e `tamanhoPagina` igual a 20

#### Scenario: Tamanho de página acima do limite
- **WHEN** um cliente envia `tamanhoPagina=500`
- **THEN** a API responde `400 Bad Request` como `ProblemDetails` indicando o parâmetro inválido

#### Scenario: Cálculo de páginas
- **WHEN** existem 45 itens e `tamanhoPagina` é 20
- **THEN** `totalItens` é 45 e `totalPaginas` é 3

### Requirement: Documentação da API
O sistema DEVE (SHALL) publicar o documento OpenAPI e a interface Swagger em `/swagger`, descrevendo endpoints, modelos e códigos de status.

#### Scenario: Swagger disponível
- **WHEN** um cliente abre `/swagger` com o container em execução
- **THEN** a interface carrega e lista todos os endpoints publicados

### Requirement: Informações da API
O sistema DEVE (SHALL) expor `GET /info`, sem autenticação, implementado pelo caso de uso `ObterInformacoesUseCase`, retornando `nome`, `versao` e `ambiente` da API, e o endpoint DEVE (SHALL) aparecer no Swagger.

#### Scenario: Consultar informações
- **WHEN** um cliente chama `GET /info`
- **THEN** a API responde `200` com `nome`, `versao` e `ambiente`

#### Scenario: Visível no Swagger
- **WHEN** o documento OpenAPI é gerado
- **THEN** ele contém a rota `/info` com resumo e modelo de resposta

### Requirement: Health Checks
O sistema DEVE (SHALL) expor `GET /health/live` (processo vivo) e `GET /health/ready` (banco acessível), ambos sem autenticação.

#### Scenario: Banco disponível
- **WHEN** o PostgreSQL está acessível
- **THEN** `/health/ready` e `/health/live` respondem `200`

#### Scenario: Banco indisponível
- **WHEN** o PostgreSQL está inacessível
- **THEN** `/health/ready` responde `503` e `/health/live` continua respondendo `200`

### Requirement: Logs Estruturados
O sistema DEVE (SHALL) escrever logs JSON no stdout com timestamp, nível, mensagem, `traceId` e caminho da requisição.

#### Scenario: Requisição registrada
- **WHEN** qualquer requisição é processada
- **THEN** uma entrada de log com método, caminho, status e tempo decorrido é escrita no stdout
