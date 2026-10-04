## Purpose

Define como a aplicação é empacotada e executada: imagem Docker, ambiente Docker Compose com PostgreSQL e configuração exclusivamente por variáveis de ambiente.

## ADDED Requirements

### Requirement: Imagem Docker da Aplicação
O sistema DEVE (SHALL) fornecer um `Dockerfile` multi-stage que restaura, compila e publica a API com a imagem `mcr.microsoft.com/dotnet/sdk:10.0` e executa na imagem `mcr.microsoft.com/dotnet/aspnet:10.0` com usuário não-root, escutando na porta 8080.

#### Scenario: Build a partir de clone limpo
- **WHEN** `docker build .` é executado em um clone limpo
- **THEN** o build conclui sem ferramentas locais e a imagem final não contém SDK nem código-fonte

#### Scenario: Execução sem root
- **WHEN** o container está em execução
- **THEN** o processo não roda como root

### Requirement: Ambiente com Docker Compose
O sistema DEVE (SHALL) fornecer `docker-compose.yml` com o serviço `postgres` (imagem `postgres:16`, volume nomeado, healthcheck com `pg_isready`) e o serviço `api` (build do Dockerfile, porta `8080:8080`) que só inicia depois de o PostgreSQL estar saudável.

#### Scenario: Subida com um comando
- **WHEN** o desenvolvedor executa `docker compose up --build`
- **THEN** PostgreSQL e API sobem, as migrações DbUp são aplicadas e `GET /health/ready` retorna `200`

#### Scenario: Persistência de dados
- **WHEN** a stack é reiniciada com `docker compose down` seguido de `up`
- **THEN** os dados gravados anteriormente continuam disponíveis

#### Scenario: Reset limpo
- **WHEN** o desenvolvedor executa `docker compose down -v`
- **THEN** o volume é removido e a próxima subida cria um banco vazio

### Requirement: Configuração por Ambiente
O sistema DEVE (SHALL) ser configurado apenas por variáveis de ambiente documentadas em `.env.example`; a fundação define `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `ConnectionStrings__Padrao`, `APLICAR_MIGRACOES` e `ASPNETCORE_ENVIRONMENT`, e cada fatia que introduzir novas variáveis DEVE (SHALL) acrescentá-las ao `.env.example`. O repositório NÃO DEVE (SHALL NOT) conter segredos reais.

#### Scenario: Arquivo de exemplo
- **WHEN** o desenvolvedor copia `.env.example` para `.env` e sobe o Compose
- **THEN** a stack funciona com valores padrão apenas para desenvolvimento

### Requirement: Higiene do Container
O repositório DEVE (SHALL) incluir `.dockerignore` excluindo `bin/`, `obj/`, `.git/`, saídas dos projetos de teste e `.env`.

#### Scenario: Contexto de build enxuto
- **WHEN** a imagem é construída
- **THEN** artefatos locais de build e o arquivo `.env` não são enviados ao daemon do Docker
