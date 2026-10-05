# Change: Implantar no Render + Neon com CI/CD no GitHub Actions

## Why
A API só roda localmente. Para ser usada e demonstrada, precisa de um endereço público com banco gerenciado, sem custo
e **sem cartão de crédito**, e de um caminho automático e seguro do commit até produção: testes em todo PR, migrações do
banco aplicadas de forma controlada e deploy só quando tudo passa.

## What Changes
- **Serviço no Render (plano grátis, sem cartão):** a API roda a partir do `Dockerfile` atual, declarada como código em
  `render.yaml` (Blueprint), com HTTPS do próprio Render e verificação de saúde em `/health/live`.
- **Banco no Neon (plano grátis, sem cartão):** PostgreSQL gerenciado com SSL obrigatório, na mesma região do serviço.
- **CI no GitHub Actions** em todo push e PR: build com *warnings* como erro, testes unitários e de integração
  (Testcontainers no runner) e build da imagem Docker.
- **CD na `main`**, em sequência e parando no primeiro erro: CI verde → **migrações aplicadas no Neon pelo pipeline**
  → deploy no Render → verificação pós-deploy (`/health/ready` e `/info` com o commit implantado).
- A API ganha um modo `--migrar` (aplica as migrações e encerra com código de saída), usado pelo pipeline;
  em produção ela sobe com `APLICAR_MIGRACOES=false`.
- `GET /info` passa a informar o commit implantado na `versao`, para o pipeline confirmar que a versão nova está no ar.
- Documentação de configuração única (contas, segredos do GitHub, variáveis do Render) no README;
  `project.md` deixa de citar Azure DevOps.

Premissas registradas:
- O Render grátis **dorme após 15 minutos sem tráfego** e leva de 30 a 60 s para acordar; o Neon grátis também desliga
  sem uso. A primeira requisição após inatividade é lenta — aceito para um ambiente de demonstração.
- Não há "ping" para manter a API acordada.
- O Grafana/Loki/Alloy continuam apenas no ambiente local; em produção os logs são lidos no painel do Render.
- O banco grátis do Neon tem 1 GB por projeto, suficiente para a API.

## Capabilities

### New Capabilities
- `entrega-continua`: pipeline de integração contínua, pipeline de entrega com migração antes do deploy, verificação
  pós-deploy, ambiente de produção no Render + Neon e gestão de segredos

### Modified Capabilities
- `persistencia`: novo requisito de migração como etapa de implantação (modo `--migrar` com código de saída)
- `convencoes-api`: "Informações da API" passa a incluir o commit implantado na `versao`

## Impact
- Arquivos novos: `.github/workflows/ci.yml`, `.github/workflows/cd.yml`, `render.yaml`.
- Código: `Program.cs` (modo `--migrar`), `InformacoesDaAplicacao` (commit na versão).
- Configuração: segredos no GitHub (`NEON_CONNECTION_STRING`, `RENDER_DEPLOY_HOOK_URL`, `URL_PRODUCAO`) e variáveis
  no Render (connection string, `JWT_*`, `AUTH_*`), nunca no repositório.
- Contas: Render e Neon (gratuitas, sem cartão), configuradas uma única vez pelo dono do repositório.
- Fluxo de trabalho: merge na `main` passa a implantar em produção.
