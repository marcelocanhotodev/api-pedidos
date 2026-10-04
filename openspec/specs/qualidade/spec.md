# qualidade Specification

## Purpose

Define a estratégia de testes e os portões de qualidade da solução, aplicados a cada fatia: testes unitários por caso de uso, testes de integração, teste de arquitetura e rastreabilidade dos cenários.

## Requirements

### Requirement: Testes Unitários por Caso de Uso
O sistema DEVE (SHALL) incluir, para cada caso de uso, uma classe de testes xUnit própria (`<Nome>UseCaseTests`) em `Pedidos.UnitTests/CasosDeUso/<Area>/`, com Moq para repositórios, `IUnitOfWork` e `IRelogio`, sem acessar banco de dados nem servidor HTTP. Cada cenário da spec do recurso correspondente DEVE (SHALL) ter ao menos um teste, e os testes DEVEM (SHALL) seguir o padrão Arrange-Act-Assert com nomes no formato `Metodo_Condicao_ResultadoEsperado`.

#### Scenario: Um arquivo de testes por caso de uso
- **WHEN** a pasta `Pedidos.UnitTests/CasosDeUso` é inspecionada
- **THEN** cada `<Nome>UseCase` possui uma classe `<Nome>UseCaseTests` correspondente

#### Scenario: Rápido e isolado
- **WHEN** `dotnet test` executa o projeto de testes unitários
- **THEN** ele conclui sem Docker nem rede

### Requirement: Testes Unitários dos Endpoints
Os endpoints DEVEM (SHALL) ser cobertos por testes de integração (não unitários) que verificam o contrato HTTP, e os validators FastEndpoints DEVEM (SHALL) ter testes unitários próprios, pois toda a lógica de negócio já está coberta nos testes dos casos de uso.

#### Scenario: Validator testado isoladamente
- **WHEN** o validator de um endpoint recebe um campo inválido
- **THEN** o teste unitário do validator verifica a mensagem de erro daquele campo

### Requirement: Testes de Integração
O sistema DEVE (SHALL) incluir testes de integração com `WebApplicationFactory` e Testcontainers (PostgreSQL), aplicando as migrações DbUp, e cada fatia DEVE (SHALL) acrescentar os testes de integração dos seus fluxos.

#### Scenario: Aplicação sobe contra banco real
- **WHEN** a suíte de integração inicia a aplicação contra um container PostgreSQL vazio
- **THEN** as migrações são aplicadas e `GET /health/ready` responde `200`

#### Scenario: Erro inesperado como ProblemDetails
- **WHEN** um teste provoca uma exceção não tratada
- **THEN** a resposta é `500` com `application/problem+json` e sem stack trace

### Requirement: Teste de Arquitetura
O sistema DEVE (SHALL) incluir um teste automatizado que verifica a regra de dependência entre as camadas e a ausência de referências a Entity Framework.

#### Scenario: Dependência proibida
- **WHEN** uma camada interna referencia uma camada externa
- **THEN** o teste de arquitetura falha

#### Scenario: Endpoint sem acesso direto a dados
- **WHEN** o teste de arquitetura inspeciona os construtores dos endpoints
- **THEN** falha se algum endpoint receber repositório, `IUnitOfWork` ou conexão em vez de `IUseCase<,>`

#### Scenario: Caso de uso sem infraestrutura
- **WHEN** o teste de arquitetura inspeciona os tipos usados pelos casos de uso
- **THEN** falha se algum referenciar Dapper, Npgsql, FastEndpoints ou `HttpContext`

### Requirement: Rastreabilidade dos Cenários
Cada cenário em `openspec/specs` DEVE (SHALL) corresponder a ao menos um teste automatizado ou constar como verificado manualmente no README do projeto, e cada fatia DEVE (SHALL) entregar os testes dos seus cenários antes de ser arquivada.

#### Scenario: Revisão de cobertura
- **WHEN** uma change está pronta para ser arquivada
- **THEN** cada cenário dela tem um teste com nome referenciando o requisito ou uma entrada no README

### Requirement: Portões de Qualidade
A solução DEVE (SHALL) compilar com `nullable` habilitado e *warnings* tratados como erro, e as classes de domínio e de casos de uso NÃO DEVEM (SHALL NOT) depender diretamente de `HttpContext`, `NpgsqlConnection` ou Dapper.

#### Scenario: Warning quebra o build
- **WHEN** um warning de compilação é introduzido
- **THEN** o build falha
