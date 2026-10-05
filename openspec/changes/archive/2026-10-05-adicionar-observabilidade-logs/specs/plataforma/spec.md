## MODIFIED Requirements

### Requirement: Ambiente com Docker Compose
O sistema DEVE (SHALL) fornecer `docker-compose.yml` com o serviço `postgres` (imagem `postgres:16`, volume nomeado, healthcheck com `pg_isready`), o serviço `api` (build do Dockerfile, porta `8080:8080`) que só inicia depois de o PostgreSQL estar saudável, e os serviços de observabilidade `loki`, `alloy` e `grafana` (porta `3000:3000`), todos subindo no mesmo comando, com imagens de versão fixada.

#### Scenario: Subida com um comando
- **WHEN** o desenvolvedor executa `docker compose up --build`
- **THEN** PostgreSQL, API, Loki, Alloy e Grafana sobem, as migrações DbUp são aplicadas, `GET /health/ready` retorna `200` e o Grafana responde em `http://localhost:3000`

#### Scenario: Persistência de dados
- **WHEN** a stack é reiniciada com `docker compose down` seguido de `up`
- **THEN** os dados gravados anteriormente continuam disponíveis

#### Scenario: Reset limpo
- **WHEN** o desenvolvedor executa `docker compose down -v`
- **THEN** o volume é removido e a próxima subida cria um banco vazio
