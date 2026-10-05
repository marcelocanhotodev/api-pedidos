## 1. Aplicação

- [x] 1.1 Extrair "esperar o banco + migrar" para `MigrarAsync` reutilizável, mantendo `AplicarMigracoesAsync` (startup com `APLICAR_MIGRACOES`) sobre ele; verificar com os testes de migração existentes verdes
- [x] 1.2 Modo `--migrar` no `Program.cs`: host mínimo sem JWT nem servidor HTTP, código de saída `0`/`1`; verificar com testes de integração que executam o processo com `--migrar` contra banco vazio (saída `0`, tabelas criadas), banco atualizado (saída `0`, nada executado) e banco inacessível (saída diferente de zero), sem `JWT_CHAVE` definido
- [x] 1.3 `InformacoesDaAplicacao` com o commit de `RENDER_GIT_COMMIT` ou `VERSAO_COMMIT` (`<versão>+<7 caracteres>`); verificar com teste unitário (com e sem commit) e de integração em `/info`

## 2. Integração contínua

- [ ] 2.1 `.github/workflows/ci.yml`: push e PR para a `main`, .NET 10 com cache do NuGet, build Release, testes unitários e de integração e `docker build`, com `concurrency` por ref; verificar que o workflow roda verde no GitHub num PR e que falha num PR de teste com um teste quebrado proposital (PR descartado depois)

## 3. Produção

- [x] 3.1 `render.yaml` (Blueprint) com o web service grátis em Docker, região `virginia`, `healthCheckPath: /health/live`, `autoDeploy: false`, `APLICAR_MIGRACOES=false`, `PORT=8080` e variáveis secretas com `sync: false`; verificar com a validação de Blueprint do Render e que o arquivo não contém nenhum valor secreto
- [ ] 3.2 Configuração única guiada pelo README (Neon em `aws-us-east-1` com `SSL Mode=Require`, serviço criado pelo Blueprint, variáveis preenchidas, Deploy Hook e três segredos no GitHub); verificar com a primeira entrega bem-sucedida (depende das contas do usuário)

## 4. Entrega contínua

- [ ] 4.1 `.github/workflows/cd.yml`: `workflow_run` do CI com sucesso na `main`, `concurrency: producao` sem cancelamento, checagem dos segredos, `--migrar` no `head_sha`, Deploy Hook com `?ref=<sha>` e verificação de `/info` (commit) e `/health/ready` com tempo limite de 10 min; verificar com uma entrega real ponta a ponta (migração aplicada, versão com o commit no ar)
- [ ] 4.2 Cenários de falha: segredo ausente falha antes da migração; migração com erro não dispara deploy; verificar no GitHub com execuções provocadas (segredo temporariamente removido; branch de teste com script inválido apontada para um banco descartável do Neon) e registrar o resultado no README

## 5. Documentação

- [x] 5.1 README: seção "Implantação" (arquitetura Render + Neon + GitHub Actions, configuração única passo a passo, segredos, cold start, regra de migrações compatíveis com a versão anterior, rollback) e cenários verificados manualmente; `openspec/project.md` passa a citar GitHub e GitHub Actions no lugar de Azure DevOps; verificar seguindo o passo a passo
