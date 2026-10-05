## Context

A API escreve logs JSON no stdout com o `JsonFormatter` do Serilog (`renderMessage: true`). Cada linha traz
`Timestamp`, `Level`, `RenderedMessage`, `TraceId`, `SpanId` e `Properties` (com `RequestMethod`, `RequestPath`,
`StatusCode` e `Elapsed` nos logs de requisição). O Compose sobe hoje `postgres` e `api`, com variáveis vindas do `.env`.
O desenvolvimento roda em Docker Desktop (Windows), onde `/var/run/docker.sock` fica disponível para os containers.

## Goals / Non-Goals

**Goals:**
- Consultar os logs da API e do banco no Grafana logo após `docker compose up`, sem configuração manual.
- Zero mudança no código da API e zero acoplamento da API à observabilidade.

**Non-Goals:**
- Métricas (Prometheus) e traces distribuídos (Tempo/OpenTelemetry): ficam para uma change futura.
- Alertas, multi-tenant do Loki, armazenamento em objeto (S3) e alta disponibilidade.
- Configuração de produção: os arquivos visam o ambiente local de desenvolvimento.

## Decisions

- **Coleta pelo stdout dos containers, com Grafana Alloy.** O Alloy descobre os containers pelo socket do Docker
  (`discovery.docker`) e lê os logs (`loki.source.docker`), exatamente o que `docker compose logs` mostra.
  A API não sabe que o Loki existe e o requisito "logs JSON no stdout" de `convencoes-api` continua sendo o contrato.
  Alternativas descartadas:
  - *Sink `Serilog.Sinks.Grafana.Loki` na API*: muda o código, acopla a API ao Loki e exige buffer/retry quando o Loki cai.
  - *Promtail*: descontinuado pela Grafana em favor do Alloy.
  - *Driver de log `loki` do Docker*: exige plugin instalado no host, o que quebra "roda só com `docker compose up`".
- **Somente containers do projeto, com nome fixo.** O Alloy filtra pelo rótulo `com.docker.compose.project`. Para não
  depender do nome da pasta do clone, o Compose declara `name: api-pedidos` e repassa `${COMPOSE_PROJECT_NAME}` ao Alloy.
  Efeito colateral: os volumes passam a ter o prefixo `api-pedidos_` (os antigos, do nome da pasta, não são apagados).
  Todos os serviços do projeto são coletados (inclusive `loki`, `alloy` e `grafana`); só a `api` recebe o pipeline JSON.
- **Posições de leitura persistidas.** O Alloy guarda em volume (`alloy-dados`) até onde leu cada container, para não
  reenviar logs ao reiniciar.
- **`service_name` a partir de `servico`.** O Loki 3 cria `service_name` automaticamente (padrão `unknown_service`);
  `discover_service_name: [servico]` o alinha ao serviço, o que também serve ao Logs Drilldown do Grafana.
- **Rótulos de baixa cardinalidade, o resto como metadado.** Rótulos (índice do Loki): `servico` (nome do serviço
  Compose) e, para a `api`, `nivel` (vindo de `Level`). `TraceId` e `RequestPath` viram metadado estruturado
  (*structured metadata*), consultável sem criar uma série por valor. `StatusCode` é extraído na consulta do painel com
  `| json`, sem indexar. Indexar `traceId` como rótulo criaria uma série por requisição e degradaria o Loki.
- **Timestamp do próprio log.** O estágio `stage.timestamp` usa o `Timestamp` do JSON da API, para que a ordem no Grafana
  seja a da aplicação e não a da coleta.
- **Loki monolítico com armazenamento em disco.** Imagem `grafana/loki` em modo único, schema TSDB, volume nomeado
  `loki-dados`, compactador com `retention_enabled` e `retention_period: 168h` (7 dias). Porta 3100 apenas na rede
  interna do Compose.
- **Grafana provisionado por arquivo.** `observabilidade/grafana/provisioning/datasources/loki.yaml` cria a fonte Loki
  como padrão (uid fixo `loki`); `.../dashboards/` carrega `api-pedidos-logs.json` com três painéis: logs recentes da
  `api` (com filtro de nível por variável), volume por nível (`sum by (nivel) (count_over_time(...))`) e requisições 5xx
  (`| json | Properties_StatusCode >= 500`). Volume `grafana-dados` guarda preferências.
- **Credenciais sem padrão.** `GF_SECURITY_ADMIN_USER`/`GF_SECURITY_ADMIN_PASSWORD` vêm de `GRAFANA_ADMIN_USUARIO`/
  `GRAFANA_ADMIN_SENHA` com `:?` no Compose (falha clara se faltar); `GF_AUTH_ANONYMOUS_ENABLED=false`.
- **Acesso ao socket do Docker só no Alloy.** O Alloy monta `/var/run/docker.sock` com `:ro`, que impede trocar o
  arquivo do socket, mas **não** restringe as chamadas à API do Docker. É o único serviço com esse acesso e o arranjo é
  apenas para desenvolvimento.
- **Ordem de subida, sem healthcheck no Loki.** A imagem do Loki é distroless (sem shell, `wget` ou `curl`) e o binário
  não tem comando de checagem, então `alloy` e `grafana` dependem apenas de `loki` iniciado: o Alloy reenvia com backoff
  até o Loki aceitar e o Grafana só consulta sob demanda. O healthcheck fica no Grafana (`/api/health`). `api` não
  depende de nenhum deles. Logs emitidos antes de o Alloy subir são lidos na primeira descoberta, então a inicialização
  da API (migrações) também aparece no Loki.
- **Versões fixadas.** Imagens de Loki, Alloy e Grafana com tag exata (sem `latest`), escolhidas e conferidas na
  implementação, registradas no `docker-compose.yml`.
- **Versões usadas:** `grafana/loki:3.7.8`, `grafana/alloy:v1.20.1` e `grafana/grafana:13.2.3`.
- **Verificação automatizada por script.** `observabilidade/verificar.sh` aguarda o Loki pronto (`/ready`, até 90 s:
  com volume novo o ingester leva alguns segundos), gera `GET /info` e consulta o Loki pelo proxy da fonte de dados do
  Grafana até encontrar o log, falhando após 30 segundos. Como o log de requisição registra o caminho sem query string,
  a busca é pelo log de `/info` emitido a partir do instante da requisição. Os testes .NET não sobem essa stack.

## Risks / Trade-offs

- [Três containers a mais na subida padrão] → escolha explícita do usuário; README informa o consumo e como parar só a
  observabilidade (`docker compose stop loki alloy grafana`).
- [Acesso ao socket do Docker dá poder sobre o host, mesmo com `:ro`] → restrito ao Alloy e ao ambiente de desenvolvimento;
  em produção a coleta seria feita por outro meio (ex.: agente do provedor ou driver de log).
- [Linhas fora do JSON da API, como avisos do runtime no stderr] → chegam ao Loki pelo serviço `api` sem rótulo `nivel`.
- [Formato do JSON da API mudar] → o pipeline do Alloy depende de `Level`, `TraceId`, `Timestamp` e `Properties`;
  o script de verificação acusa se a consulta parar de encontrar os logs.
- [Docker Desktop em outro SO/caminho do socket] → caminho padrão documentado; Linux e macOS com Docker Desktop usam o mesmo.

## Migration Plan

Mudança aditiva. Quem já tem um `.env` precisa acrescentar `GRAFANA_ADMIN_USUARIO` e `GRAFANA_ADMIN_SENHA` (ou recopiar
o `.env.example`); sem elas o `docker compose up` falha com a mensagem da variável. Para voltar atrás, basta remover os
três serviços do Compose: a API não muda.
