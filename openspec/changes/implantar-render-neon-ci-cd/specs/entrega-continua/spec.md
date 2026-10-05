## Purpose

Leva cada mudança do repositório até produção de forma automática e segura: valida todo push e pull request, aplica as migrações do banco antes de publicar a versão nova e confirma que ela está no ar, num ambiente gratuito sem cartão de crédito.

## ADDED Requirements

### Requirement: Integração Contínua
O sistema DEVE (SHALL) executar, em todo push e em todo pull request para a `main`, um pipeline que compila a solução com *warnings* tratados como erro, roda os testes unitários e de integração (com PostgreSQL via Testcontainers) e constrói a imagem Docker; qualquer falha DEVE (SHALL) marcar o pipeline como falho.

#### Scenario: Pull request válido
- **WHEN** um pull request com código que compila e passa nos testes é aberto
- **THEN** o pipeline de integração termina com sucesso, executando build, testes unitários, testes de integração e build da imagem

#### Scenario: Teste quebrado
- **WHEN** um pull request faz algum teste falhar
- **THEN** o pipeline termina com falha e indica o teste que falhou

#### Scenario: Imagem que não constrói
- **WHEN** uma mudança impede o `docker build` de concluir
- **THEN** o pipeline termina com falha

### Requirement: Entrega Contínua com Migração Antes do Deploy
Após a integração contínua passar num push para a `main`, o sistema DEVE (SHALL) executar, em sequência e parando no primeiro erro: (1) aplicação das migrações pendentes no banco de produção, (2) deploy da versão no Render e (3) verificação pós-deploy. Uma falha em qualquer etapa NÃO DEVE (SHALL NOT) executar as etapas seguintes, e duas execuções de entrega NÃO DEVEM (SHALL NOT) rodar ao mesmo tempo.

#### Scenario: Entrega bem-sucedida
- **WHEN** um commit chega à `main` e a integração contínua passa
- **THEN** as migrações são aplicadas no Neon, o Render implanta o commit e a verificação pós-deploy confirma a versão nova

#### Scenario: Migração com erro bloqueia o deploy
- **WHEN** uma migração falha no banco de produção
- **THEN** o pipeline termina com falha, o deploy não é disparado e a versão anterior continua no ar

#### Scenario: CI falho não entrega
- **WHEN** a integração contínua falha num push para a `main`
- **THEN** nenhuma migração é aplicada e nenhum deploy é disparado

#### Scenario: Entregas simultâneas
- **WHEN** dois commits chegam à `main` em sequência rápida
- **THEN** as entregas rodam uma de cada vez, na ordem dos commits

### Requirement: Verificação Pós-Deploy
O pipeline de entrega DEVE (SHALL) aguardar a versão nova responder em produção, confirmando que `GET /info` informa o commit implantado e que `GET /health/ready` responde `200`, com tempo limite que acomode o despertar do plano gratuito; ao estourar o limite, o pipeline DEVE (SHALL) falhar.

#### Scenario: Versão nova confirmada
- **WHEN** o deploy termina e a API acorda
- **THEN** `/info` informa o commit do pipeline, `/health/ready` responde `200` e o pipeline termina com sucesso

#### Scenario: Versão nova não sobe
- **WHEN** a versão implantada não passa a responder com o commit esperado dentro do tempo limite
- **THEN** o pipeline termina com falha indicando que a verificação pós-deploy não confirmou a versão

### Requirement: Ambiente de Produção Gratuito
A API DEVE (SHALL) ser hospedada no plano gratuito do Render a partir do `Dockerfile` do repositório, declarada em `render.yaml`, com HTTPS fornecido pela plataforma, verificação de saúde em `/health/live` e `APLICAR_MIGRACOES=false`; o banco DEVE (SHALL) ser um PostgreSQL gratuito do Neon com SSL obrigatório. Nenhum dos dois DEVE (SHALL) exigir cartão de crédito.

#### Scenario: API pública com HTTPS
- **WHEN** um cliente chama `https://<serviço>.onrender.com/info`
- **THEN** a API responde `200` por HTTPS

#### Scenario: Banco exige SSL
- **WHEN** a API conecta ao Neon
- **THEN** a conexão usa SSL, e uma conexão sem SSL é recusada

#### Scenario: Despertar após inatividade
- **WHEN** a API ficou mais de 15 minutos sem tráfego e recebe uma requisição
- **THEN** a requisição é atendida depois que o serviço e o banco acordam, sem erro, ainda que com atraso

#### Scenario: Saúde sem depender do banco
- **WHEN** o Render verifica a saúde do serviço enquanto o banco ainda está acordando
- **THEN** a verificação usa `/health/live`, que não depende do banco, e o serviço não é reiniciado por isso

### Requirement: Segredos Fora do Repositório
Connection string de produção, chave JWT, credenciais de autenticação e URL de deploy DEVEM (SHALL) ficar apenas nos segredos do GitHub e nas variáveis de ambiente do Render; o `render.yaml` e os workflows NÃO DEVEM (SHALL NOT) conter valores secretos, e os logs do pipeline NÃO DEVEM (SHALL NOT) exibi-los.

#### Scenario: Repositório sem segredos
- **WHEN** o repositório é inspecionado
- **THEN** `render.yaml` declara as variáveis secretas sem valor e os workflows só as leem de `secrets`

#### Scenario: Segredo ausente
- **WHEN** o pipeline de entrega roda sem algum segredo obrigatório configurado
- **THEN** ele falha antes de qualquer migração ou deploy, indicando o nome do segredo ausente
