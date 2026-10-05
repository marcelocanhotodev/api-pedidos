## Context

A API roda hoje só via `docker compose` local (PostgreSQL 16 + API + Loki/Alloy/Grafana). O repositório está no GitHub
(`marcelocanhotodev/api-pedidos`), sem pipeline. A API já: sobe a partir do `Dockerfile` multi-stage (porta 8080,
usuário não-root), aplica migrações DbUp no startup quando `APLICAR_MIGRACOES=true`, espera o banco por até 30 s,
expõe `/health/live` (não depende do banco) e `/health/ready` (depende), e recusa iniciar sem `JWT_CHAVE` válida.
Requisito do usuário: hospedagem **gratuita e sem cartão de crédito**.

## Goals / Non-Goals

**Goals:**
- Endereço público HTTPS para a API e banco PostgreSQL gerenciado, sem custo e sem cartão.
- Todo push/PR validado; toda entrega na `main` com migração controlada antes do deploy e confirmação depois.
- Infraestrutura do serviço declarada no repositório (`render.yaml`) e nenhum segredo versionado.

**Non-Goals:**
- Eliminar o *cold start* do plano gratuito (sem "ping" para manter acordado).
- Observabilidade em produção com Grafana: o stack Loki/Alloy/Grafana segue só no ambiente local; produção usa o painel
  de logs do Render. Grafana Cloud fica para uma change futura.
- Ambientes de homologação ou previews por pull request (branches do Neon, preview environments do Render).
- Rollback automatizado (ver Migration Plan).

## Decisions

### Plataformas
- **Render (Web Service, plano `free`, runtime Docker)** — sem cartão, aceita o `Dockerfile` atual sem mudanças, HTTPS
  automático em `*.onrender.com`, 512 MB de RAM e 750 h/mês (um serviço cabe). Dorme após 15 min sem tráfego.
  Alternativas descartadas: Cloud Run, Azure Container Apps e Oracle Always Free exigem cartão; Koyeb passou a exigir
  cartão em 2026; Fly.io e Railway não têm plano gratuito permanente.
- **Neon (plano `free`)** — PostgreSQL gerenciado sem cartão, 1 GB por projeto, desliga sem uso e volta sozinho.
  Alternativa descartada: o PostgreSQL grátis do Render **expira em 30 dias** e é apagado depois.
- **Região:** Render em `virginia` e Neon em `aws-us-east-1`, a mesma região física. O Render não tem região no Brasil.

### Serviço declarado em `render.yaml` (Blueprint)
- `type: web`, `runtime: docker`, `plan: free`, `region: virginia`, `dockerfilePath: ./Dockerfile`,
  `healthCheckPath: /health/live`, `autoDeploy: false` (quem decide o deploy é o pipeline, depois da migração).
- `envVars`: `PORT=8080`, `ASPNETCORE_ENVIRONMENT=Production`, `APLICAR_MIGRACOES=false`,
  `JWT_EMISSOR`/`JWT_AUDIENCIA`/`JWT_EXPIRA_MINUTOS` com valores fixos e `ConnectionStrings__Padrao`, `JWT_CHAVE`,
  `AUTH_USUARIO`, `AUTH_SENHA` com `sync: false` (sem valor no arquivo; preenchidos uma vez no painel).
- Saúde por `/health/live`: com `/health/ready`, o Render poderia reiniciar o serviço enquanto o Neon acorda.

### Conexão com o Neon
- Endpoint **direto** (não o `-pooler`): com uma única instância, o pool do Npgsql basta e evita as restrições do
  PgBouncer em modo *transaction*.
- Connection string no formato do Npgsql, com `SSL Mode=Require` e `Maximum Pool Size=10`, para ficar folgado nos
  limites de conexão do plano gratuito. O tempo de despertar do Neon (alguns segundos) cabe na espera de 30 s que
  a API já faz.

### Pipelines (GitHub Actions)
- **`ci.yml`** (`push` e `pull_request` para a `main`; `concurrency` cancela execuções antigas do mesmo ref):
  `actions/setup-dotnet` com .NET 10, cache do NuGet, `dotnet build -c Release` (*warnings* já são erro),
  `dotnet test` (o runner `ubuntu-latest` tem Docker, então Testcontainers funciona sem ajuste) e `docker build`.
- **`cd.yml`** (`workflow_run` do CI concluído com sucesso na `main`; `concurrency: producao` sem cancelamento, para as
  entregas rodarem uma de cada vez, na ordem):
  1. **Checagem de segredos**: falha cedo, com o nome do segredo ausente, antes de tocar no banco.
  2. **Migração**: `dotnet run --project src/Pedidos.Api -c Release -- --migrar` com
     `ConnectionStrings__Padrao` vindo de `secrets.NEON_CONNECTION_STRING`, no commit exato que passou no CI
     (`workflow_run.head_sha`).
  3. **Deploy**: `POST` no Deploy Hook do Render (`secrets.RENDER_DEPLOY_HOOK_URL`) com `?ref=<sha>`, para implantar
     exatamente o commit migrado, mesmo que a `main` já tenha avançado.
  4. **Verificação**: a cada 10 s, por até 10 min (build no Render + despertar), lê `GET <URL_PRODUCAO>/info` até a
     `versao` terminar em `+<sha curto>` e então exige `GET /health/ready` = `200`. Estourou o tempo: falha.
- **Segredos do repositório** (não de *environment*, que no plano gratuito do GitHub não oferece segredos para
  repositórios privados): `NEON_CONNECTION_STRING`, `RENDER_DEPLOY_HOOK_URL`, `URL_PRODUCAO`. O GitHub mascara os
  valores nos logs e os workflows nunca os imprimem.

### Modo `--migrar`
- Em `Program.cs`, quando os argumentos contêm `--migrar`, a aplicação monta um host mínimo
  (`Host.CreateApplicationBuilder`, logs JSON, `AddInfrastructure`), executa a espera pelo banco e o `MigradorDeBanco`
  e retorna `0` ou `1`, **sem** `AddAutenticacaoJwt` (o pipeline não precisa de JWT) e sem abrir porta HTTP.
- A lógica de "esperar + migrar" sai de `AplicarMigracoesAsync` para um método reutilizável (`MigrarAsync`), chamado
  pelo startup normal (quando `APLICAR_MIGRACOES=true`) e pelo modo `--migrar`.
- Alternativa descartada: migrar no startup em produção (`APLICAR_MIGRACOES=true`) — uma migração com erro derrubaria a
  versão nova já no Render, e o pipeline não teria como impedir o deploy.

### Ordem migração → deploy e compatibilidade
- A migração roda **antes** de a versão nova subir; durante alguns minutos a versão **anterior** atende sobre o esquema
  **novo**. Por isso toda migração deve ser compatível com a versão anterior do código (*expand/contract*): adicionar
  colunas/tabelas é seguro; remover ou renomear exige duas entregas (primeiro o código para de usar, depois a migração
  remove). A regra entra no README.

### Commit na versão
- `InformacoesDaAplicacao` lê `RENDER_GIT_COMMIT` (definida pelo próprio Render a cada deploy) ou, na falta dela,
  `VERSAO_COMMIT`, e monta `versao = <versão do assembly>+<7 primeiros caracteres>`. É isso que a verificação compara.

## Risks / Trade-offs

- [Cold start de 30–60 s no Render e despertar do Neon] → aceito para demonstração; `/health/live` evita reinícios; a
  verificação pós-deploy tem 10 min de tolerância. Documentado no README.
- [Planos gratuitos mudam sem aviso (como Koyeb e Render Postgres em 2026)] → `render.yaml` e connection string isolam
  a dependência; trocar de provedor é mudar segredos e o arquivo do Blueprint.
- [Deploy Hook vaza → qualquer um dispara deploys] → só implanta commits existentes do repositório; a URL fica em
  segredo e pode ser regenerada no Render.
- [Migração aplicada e deploy falha] → o banco fica no esquema novo com o código anterior no ar, o que é seguro pela
  regra de compatibilidade; o pipeline marca falha para correção com um novo commit.
- [Limite de 1 GB e de CU-hours do Neon] → muito acima do uso de uma API de demonstração; o painel do Neon mostra o consumo.

## Migration Plan

1. Configuração única (dono do repositório): criar o projeto no Neon (`aws-us-east-1`); criar o serviço no Render pelo
   Blueprint (`render.yaml`) e preencher as variáveis `sync: false`; copiar o Deploy Hook; cadastrar os três segredos
   no GitHub. Passo a passo no README.
2. Primeira entrega: merge na `main` → CI → `--migrar` cria o esquema no Neon a partir do zero → deploy → verificação.
3. Rollback: o código volta pelo botão "Rollback" do Render (deploy anterior) ou por `git revert` + nova entrega;
   migrações são só para frente, então desfazer um esquema exige uma migração nova.
