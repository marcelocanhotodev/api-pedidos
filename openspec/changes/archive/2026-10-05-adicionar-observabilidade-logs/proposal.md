# Change: Adicionar observabilidade de logs com Grafana

## Why
Hoje os logs da API só podem ser lidos com `docker compose logs`, um texto JSON corrido, sem busca, filtro por nível
ou rastreio de uma requisição pelo `traceId`. A API já escreve logs JSON estruturados no stdout; falta um lugar para
consultá-los. Grafana + Loki permitem filtrar, correlacionar e visualizar esses logs localmente, com a mesma stack
usada em produção por muitas equipes.

## What Changes
- Três novos serviços no `docker-compose.yml`, subindo **sempre** junto com `postgres` e `api`:
  - **Loki**: armazena os logs (volume nomeado, retenção de 7 dias);
  - **Grafana Alloy**: lê o stdout dos containers do projeto pelo Docker e envia ao Loki;
  - **Grafana**: interface em `http://localhost:3000`, com o Loki já configurado como fonte de dados e um painel
    "API de Pedidos — Logs" provisionado.
- Nenhuma mudança no código da API: a coleta usa os logs JSON que já vão para o stdout. Se Loki ou Alloy caírem,
  a API continua funcionando.
- Novas variáveis `GRAFANA_ADMIN_USUARIO` e `GRAFANA_ADMIN_SENHA` no `.env.example` (sem credencial padrão `admin/admin`).
- O README ganha a seção de observabilidade com exemplos de consulta (por nível, por `traceId`, erros 5xx).

Ferramentas de observabilidade não são dependências da aplicação: a restrição "um serviço, um banco" continua valendo
para a API.

## Capabilities

### New Capabilities
- `observabilidade`: coleta dos logs dos containers, consulta e painel no Grafana, retenção e credenciais

### Modified Capabilities
- `plataforma`: "Ambiente com Docker Compose" passa a incluir Loki, Alloy e Grafana na subida padrão

## Impact
- Arquivos novos: configuração do Loki, do Alloy e do provisionamento do Grafana (fonte de dados e painel) em `observabilidade/`.
- `docker-compose.yml`: três serviços, dois volumes nomeados e acesso somente leitura ao socket do Docker para o Alloy.
- `.env.example`, `README.md`.
- Recursos: três containers a mais na subida padrão (mais memória e tempo de inicialização).
- Código da API: nenhum.
