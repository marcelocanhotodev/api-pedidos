# observabilidade Specification

## Purpose

Permite consultar, filtrar e correlacionar os logs da aplicação pelo Grafana, sem alterar o código da API e sem tornar a API dependente da infraestrutura de observabilidade.

## Requirements

### Requirement: Coleta de Logs dos Containers
O sistema DEVE (SHALL) coletar os logs escritos no stdout pelos containers do projeto (`api` e `postgres`) e enviá-los ao Loki, identificados pelo serviço de origem, sem exigir mudanças no código da API. Nos logs da `api`, o nível DEVE (SHALL) ficar disponível como rótulo de filtro e o `traceId` e o caminho da requisição DEVEM (SHALL) ficar disponíveis para consulta.

#### Scenario: Requisição aparece no Loki
- **WHEN** um cliente chama `GET /info` com a stack em execução
- **THEN** em até 30 segundos o log da requisição, com método, caminho, status e tempo, pode ser consultado no Loki pelo serviço `api`

#### Scenario: Logs do banco coletados
- **WHEN** o PostgreSQL escreve no stdout
- **THEN** a entrada pode ser consultada no Loki pelo serviço `postgres`

#### Scenario: Somente containers do projeto
- **WHEN** outros containers, de fora deste projeto Compose, estão em execução na mesma máquina
- **THEN** os logs deles não são enviados ao Loki

### Requirement: Independência da API
A API NÃO DEVE (SHALL NOT) depender de Loki, Alloy ou Grafana para iniciar ou atender requisições, e os logs DEVEM (SHALL) continuar disponíveis no stdout mesmo com a coleta parada.

#### Scenario: Loki indisponível
- **WHEN** o container do Loki é parado e um cliente chama `GET /info`
- **THEN** a API responde `200` e o log continua aparecendo em `docker compose logs api`

#### Scenario: Subida sem dependência
- **WHEN** a stack sobe
- **THEN** o serviço `api` não declara dependência de `loki`, `alloy` ou `grafana`

### Requirement: Consulta no Grafana
O sistema DEVE (SHALL) expor o Grafana em `http://localhost:3000`, com acesso apenas autenticado, e com o Loki configurado automaticamente como fonte de dados padrão, sem passos manuais após a subida.

#### Scenario: Fonte de dados pronta
- **WHEN** o desenvolvedor entra no Grafana depois de `docker compose up`
- **THEN** a fonte de dados Loki já existe, é a padrão e responde ao teste de conexão

#### Scenario: Filtro por nível
- **WHEN** o desenvolvedor consulta os logs do serviço `api` filtrando o nível `Error`
- **THEN** retornam apenas as entradas com nível `Error`

#### Scenario: Rastreio por traceId
- **WHEN** o desenvolvedor consulta pelo `traceId` presente em uma resposta `ProblemDetails`
- **THEN** retornam as entradas de log daquela requisição

#### Scenario: Acesso anônimo bloqueado
- **WHEN** alguém abre o Grafana sem login
- **THEN** é direcionado para a tela de login e não vê dados

### Requirement: Painel de Logs Provisionado
O sistema DEVE (SHALL) provisionar no Grafana o painel "API de Pedidos — Logs", com os logs recentes da API, o volume de logs por nível ao longo do tempo e as requisições que responderam com status 5xx.

#### Scenario: Painel disponível após a subida
- **WHEN** o desenvolvedor entra no Grafana depois de `docker compose up` em um ambiente novo
- **THEN** o painel "API de Pedidos — Logs" já existe e exibe os logs da API

#### Scenario: Erros 5xx destacados
- **WHEN** a API responde uma requisição com status `500`
- **THEN** a requisição aparece no painel de requisições com erro

### Requirement: Retenção e Persistência dos Logs
O Loki DEVE (SHALL) guardar os logs em volume nomeado, mantendo-os entre reinícios da stack, e DEVE (SHALL) descartar automaticamente logs com mais de 7 dias.

#### Scenario: Logs preservados entre reinícios
- **WHEN** a stack é reiniciada com `docker compose down` seguido de `up`
- **THEN** os logs coletados antes do reinício continuam consultáveis

#### Scenario: Reset limpo
- **WHEN** o desenvolvedor executa `docker compose down -v`
- **THEN** os logs armazenados são removidos junto com os demais volumes

### Requirement: Credenciais do Grafana
O usuário e a senha de administrador do Grafana DEVEM (SHALL) vir das variáveis `GRAFANA_ADMIN_USUARIO` e `GRAFANA_ADMIN_SENHA`, documentadas em `.env.example` com valores apenas de desenvolvimento, e o Compose NÃO DEVE (SHALL NOT) subir o Grafana com a credencial padrão `admin/admin`.

#### Scenario: Variável ausente
- **WHEN** o `.env` não define `GRAFANA_ADMIN_SENHA`
- **THEN** o `docker compose up` falha com mensagem indicando a variável

#### Scenario: Login com as credenciais configuradas
- **WHEN** o desenvolvedor entra com `GRAFANA_ADMIN_USUARIO` e `GRAFANA_ADMIN_SENHA`
- **THEN** o login é aceito sem exigir troca de senha
