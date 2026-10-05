## Context

Hoje `GerarTokenEndpoint` usa `Group<ApiV1>()` com `AllowAnonymous()`, resultando em `POST /api/v1/auth/token`.
Dois testes sobre as rotas registradas (`RegrasDeRotas`) dependem disso:
- `AnonimasIndevidas` aceita uma única rota anônima sob `/api/v1`: a do token;
- `ForaDoGrupoApiV1` exige que todo endpoint de `Pedidos.Api.Endpoints` esteja sob `/api/v1`, exceto os de `Sistema`.

## Goals / Non-Goals

**Goals:**
- Token em `POST /auth/token`, estável entre versões do contrato de negócio.
- Regra de segurança mais simples e mais forte: nenhuma rota anônima sob `/api/v1`.

**Non-Goals:**
- Manter `POST /api/v1/auth/token` como alias ou redirecionamento.
- Mudar emissão, validação ou formato do token.

## Decisions

- **Endpoint fora do grupo.** `GerarTokenEndpoint` passa a declarar `Post("auth/token")` sem `Group<ApiV1>()`, como já
  faz `ObterInformacoesEndpoint` com `/info`. A rota antiga deixa de ser registrada e cai no `404` padrão.
  Alternativa descartada: manter as duas rotas por um período — não há consumidores externos que justifiquem o alias.
- **Anônimo sob `/api/v1` passa a ser proibido sem exceção.** `RegrasDeRotas.AnonimasIndevidas` remove a exceção do
  token: qualquer rota `/api/v1/*` com `IAllowAnonymous` é violação. A constante passa a ser `/auth/token` e serve só
  para os testes que confirmam a rota pública.
- **Endpoints públicos fora do grupo por namespace.** `ForaDoGrupoApiV1` passa a isentar `Pedidos.Api.Endpoints.Auth`
  além de `Pedidos.Api.Endpoints.Sistema`. A isenção continua explícita no código do teste: um endpoint de negócio novo
  em qualquer outro namespace ainda é obrigado a usar `Group<ApiV1>()`.
  Alternativa descartada: mover o endpoint de token para `Sistema` — mistura autenticação com informações operacionais.

## Risks / Trade-offs

- [Cliente que chama a rota antiga recebe `404`] → mudança marcada como BREAKING; README e `requests.http` atualizados.
- [Isenção por namespace pode ser usada para fugir do grupo] → limitada a `Auth` e `Sistema` no próprio teste; qualquer
  novo namespace isento exige alterar o teste, visível em revisão.
