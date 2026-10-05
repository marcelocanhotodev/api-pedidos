## 1. Loki

- [x] 1.1 `observabilidade/loki/config.yaml` em modo monolítico (schema TSDB, armazenamento em disco, compactador com retenção de 168h, `service_name` a partir de `servico`) e serviço `loki` no Compose com tag fixada e volume `loki-dados` (imagem distroless: sem healthcheck próprio); verificar que `/ready` responde `ready` pelo proxy do Grafana

## 2. Coleta com Alloy

- [x] 2.1 `observabilidade/alloy/config.alloy`: descoberta Docker filtrada pelo projeto Compose, rótulo `servico`, pipeline JSON para a `api` (rótulo `nivel`, metadado estruturado `traceId` e `caminho`, timestamp do log) e envio ao Loki; serviço `alloy` com tag fixada, nome fixo do projeto Compose, volume de posições, socket do Docker e dependência de `loki`; verificar que logs da `api` e do `postgres` chegam ao Loki e que um container fora do projeto não chega

## 3. Grafana

- [x] 3.1 Provisionamento da fonte Loki (padrão, uid `loki`) e serviço `grafana` com tag fixada, porta `3000:3000`, volume `grafana-dados`, anônimo desabilitado e credenciais de `GRAFANA_ADMIN_USUARIO`/`GRAFANA_ADMIN_SENHA` com `:?`; verificar login, teste da fonte de dados e redirecionamento para login sem autenticação
- [x] 3.2 Painel provisionado "API de Pedidos — Logs" (logs recentes com filtro de nível, volume por nível, requisições 5xx); verificar que aparece em ambiente novo e que um erro 500 provocado aparece no painel de erros

## 4. Configuração

- [x] 4.1 `GRAFANA_ADMIN_USUARIO` e `GRAFANA_ADMIN_SENHA` no `.env.example` (valores só de desenvolvimento); verificar que o `docker compose up` falha com mensagem clara sem a senha e sobe com o `.env` recopiado

## 5. Verificação

- [x] 5.1 `observabilidade/verificar.sh`: gera `GET /info` e consulta o Loki pelo proxy do Grafana até achar o log (limite de 30 s); verificar executando-o com a stack no ar
- [x] 5.2 Cenários de resiliência e persistência: com `loki` parado a API responde `200` e o log segue em `docker compose logs api`; logs preservados após `down`/`up`; removidos após `down -v`; rastreio por `traceId` de uma resposta `ProblemDetails`; verificar manualmente e registrar no README
- [x] 5.3 Suíte .NET continua passando sem depender da stack de observabilidade; verificar com `dotnet test`

## 6. Documentação

- [x] 6.1 README: seção de observabilidade (acesso, credenciais, consultas LogQL de exemplo por nível, `traceId` e 5xx, consumo de recursos e como parar só a observabilidade), decisões e cenários verificados manualmente; verificar seguindo o README
