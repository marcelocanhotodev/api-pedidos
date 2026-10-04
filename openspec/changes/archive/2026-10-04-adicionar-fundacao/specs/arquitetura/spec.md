## Purpose

Define a organização em camadas da solução, a regra de dependência da Clean Architecture e as convenções de casos de uso, repositórios e endpoints que todas as fatias seguem.

## ADDED Requirements

### Requirement: Estrutura em Camadas
O sistema DEVE (SHALL) ser organizado em quatro projetos de produção — `Pedidos.Domain`, `Pedidos.Application`, `Pedidos.Infrastructure` e `Pedidos.Api` — respeitando a regra de dependência da Clean Architecture: Domain não referencia nenhum outro projeto; Application referencia apenas Domain; Infrastructure referencia Application e Domain; Api referencia Application e Infrastructure (somente para composição de dependências).

#### Scenario: Domain isolado
- **WHEN** as referências do projeto `Pedidos.Domain` são inspecionadas
- **THEN** ele não referencia outros projetos nem pacotes de infraestrutura (Dapper, Npgsql, FastEndpoints)

#### Scenario: Violação detectada
- **WHEN** um teste de arquitetura verifica as dependências entre assemblies
- **THEN** o teste falha se Application referenciar Infrastructure ou Api

### Requirement: Repository Pattern
O sistema DEVE (SHALL) acessar dados de escrita exclusivamente por interfaces de repositório — uma por agregado, nomeada `I<Agregado>Repository` — declaradas em `Pedidos.Domain` e implementadas em `Pedidos.Infrastructure` com Dapper, sendo vedado o uso de SQL ou de `NpgsqlConnection` fora da camada Infrastructure.

#### Scenario: Caso de uso depende de abstração
- **WHEN** um caso de uso da Application precisa persistir um agregado
- **THEN** ele usa a interface de repositório do agregado e `IUnitOfWork` recebidos por injeção de dependência

#### Scenario: Teste unitário sem banco
- **WHEN** um caso de uso é testado
- **THEN** os repositórios são substituídos por mocks e nenhum banco é necessário

### Requirement: Um Caso de Uso por Endpoint
O sistema DEVE (SHALL) implementar a lógica de cada endpoint em uma classe de caso de uso própria e exclusiva na camada Application, nomeada `<Verbo><Recurso>UseCase` (ex.: `CriarClienteUseCase`), que implementa `IUseCase<TEntrada, TSaida>` e expõe um único método público `ExecutarAsync(TEntrada entrada, CancellationToken ct)`. Cada caso de uso DEVE (SHALL) ter seus próprios tipos `<Nome>Entrada` e `<Nome>Saida` (ou `Vazio` quando não há retorno), e o endpoint DEVE (SHALL) apenas converter a requisição HTTP em entrada, chamar o caso de uso e converter a saída em resposta HTTP.

#### Scenario: Endpoint fino
- **WHEN** qualquer endpoint de negócio é executado
- **THEN** ele chama um único `IUseCase<TEntrada, TSaida>` e devolve o resultado, sem regra de negócio, sem repositório e sem SQL

#### Scenario: Um endpoint, um caso de uso
- **WHEN** os endpoints são comparados aos casos de uso
- **THEN** cada endpoint depende de exatamente um caso de uso e cada caso de uso é usado por exatamente um endpoint

#### Scenario: Teste unitário focado
- **WHEN** um caso de uso é testado
- **THEN** basta instanciá-lo com mocks de repositórios e `IUnitOfWork` e chamar `ExecutarAsync`, sem servidor HTTP nem banco

### Requirement: Regras dos Casos de Uso
Os casos de uso NÃO DEVEM (SHALL NOT) depender de `HttpContext`, FastEndpoints, Dapper, `NpgsqlConnection` nem de outro caso de uso; lógica compartilhada DEVE (SHALL) ficar no Domain ou em serviços de domínio. Casos de uso de escrita DEVEM (SHALL) controlar a transação por `IUnitOfWork`, e todas as dependências (repositórios, `IUnitOfWork`, relógio, hash, emissor de token) DEVEM (SHALL) ser recebidas pelo construtor, incluindo o relógio (`IRelogio`) para que datas sejam testáveis.

#### Scenario: Sem dependência entre casos de uso
- **WHEN** o construtor de qualquer caso de uso é inspecionado
- **THEN** nenhum parâmetro é de outro caso de uso

#### Scenario: Data controlada no teste
- **WHEN** um caso de uso define uma data (ex.: `criadoEm`)
- **THEN** o valor vem de `IRelogio`, permitindo asserção determinística no teste

### Requirement: Convenção dos Casos de Uso
Todo caso de uso DEVE (SHALL) residir em `Pedidos.Application/CasosDeUso/<Area>/<NomeDoCasoDeUso>/`, com os arquivos `<Nome>UseCase.cs`, `<Nome>Entrada.cs` e `<Nome>Saida.cs`. A lista de casos de uso de cada recurso DEVE (SHALL) ser declarada na spec desse recurso, e não em um catálogo central.

#### Scenario: Convenção verificada por reflexão
- **WHEN** o teste de arquitetura inspeciona todas as classes que implementam `IUseCase<,>`
- **THEN** falha se alguma não terminar em `UseCase`, estiver fora de `CasosDeUso/<Area>/<Nome>/` ou não tiver `Entrada` e `Saida` próprias

#### Scenario: Novo recurso
- **WHEN** uma fatia adiciona um novo endpoint
- **THEN** ela traz um novo caso de uso `<Verbo><Recurso>UseCase` com `Entrada` e `Saida` próprias, sem alterar requisitos de arquitetura

### Requirement: Registro dos Casos de Uso
O sistema DEVE (SHALL) registrar todos os casos de uso automaticamente por varredura do assembly da Application (`IUseCase<,>` com tempo de vida `Scoped`) em `AddApplication`, e os endpoints DEVEM (SHALL) receber `IUseCase<TEntrada, TSaida>` por injeção de dependência.

#### Scenario: Novo caso de uso registrado
- **WHEN** uma nova classe implementa `IUseCase<,>` na Application
- **THEN** ela fica disponível para injeção sem alterar `Program.cs`

### Requirement: Um Endpoint por Classe
A camada Api DEVE (SHALL) implementar cada rota como uma classe FastEndpoints própria, com tipos de `Request` e `Response` explícitos, `Validator` quando houver entrada e metadados de documentação (`Summary`, códigos de resposta).

#### Scenario: Endpoint documentado
- **WHEN** o Swagger é gerado
- **THEN** cada endpoint exibe resumo, modelos de entrada e saída e códigos de status possíveis

### Requirement: Injeção de Dependência
O sistema DEVE (SHALL) registrar as dependências por métodos de extensão por camada (`AddApplication`, `AddInfrastructure`) invocados em `Program.cs`, usando tempo de vida `Scoped` para `IDbSession`, `IUnitOfWork`, repositórios e casos de uso.

#### Scenario: Escopo por requisição
- **WHEN** duas requisições simultâneas são processadas
- **THEN** cada uma usa sua própria conexão e transação
