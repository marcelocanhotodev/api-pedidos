## Context

A fundação (arquivada) entrega o contrato `IUseCase<,>`, o tratador global de erros (`UseExceptionHandler` +
`UseStatusCodePages`, tudo em `application/problem+json`), o Swagger e a rota anônima `GET /info`, fora de qualquer
prefixo. Esta fatia adiciona o primeiro endpoint `/api/v1` e a política de autenticação que todas as fatias seguintes herdam.

## Goals / Non-Goals

**Goals:**
- Autenticação JWT simétrica (HS256) com usuário único configurado por ambiente.
- Proteção como padrão: um endpoint novo de `/api/v1` é protegido sem precisar declarar nada.

**Non-Goals:**
- Cadastro de usuários, papéis/permissões, refresh token, OAuth/OIDC.
- Proteger `GET /info`, `/health/*` e `/swagger`, que continuam anônimos (fora de `/api/v1`).

## Decisions

- **Prefixo `/api/v1` por grupo, não global.** Um grupo FastEndpoints `ApiV1` (prefixo `api/v1`) é usado pelos
  endpoints de negócio com `Group<ApiV1>()`. Um `RoutePrefix` global com versionamento por padrão moveria `GET /info`
  para `/api/v1/info`, quebrando a spec arquivada de `convencoes-api`.
  Um teste sobre as rotas registradas exige que todo endpoint de `Pedidos.Api.Endpoints` (exceto `Sistema`) esteja sob
  `/api/v1`; como `Group<ApiV1>()` também é chamado em `Configure()`, não é verificável por reflexão nos tipos.
  Alternativa descartada: prefixo global com `RoutePrefixOverride("")` na `/info` — vira exceção espalhada por endpoint.
- **Proteção por padrão, exceção explícita.** O FastEndpoints exige usuário autenticado em todo endpoint que não chama
  `AllowAnonymous()`. Em `/api/v1`, apenas `POST /api/v1/auth/token` é anônimo.
  Alternativa descartada: marcar cada endpoint como protegido — um esquecimento abriria o endpoint.
- **Anônimo indevido verificado nas rotas registradas, não por reflexão.** `AllowAnonymous()` e a rota são definidos em
  `Configure()`, em tempo de execução, e não aparecem nos tipos. Um teste de integração sobe a aplicação, percorre o
  `EndpointDataSource` e falha se alguma rota `/api/v1/*` além de `auth/token` tiver metadado `IAllowAnonymous`.
- **401 de credenciais inválidas por exceção mapeada.** `GerarTokenUseCase` lança `CredenciaisInvalidasException`
  (em `Application/Excecoes`, pois não é regra de domínio), e o `MapeamentoDeExcecoes` a converte em `401`.
  O endpoint continua fino e todo erro sai pelo tratador global.
  Alternativa descartada: saída "falhou" traduzida no endpoint — põe decisão no endpoint.
  O 401 de "sem token" sai do middleware de autenticação sem corpo e o `UseStatusCodePages` existente o converte em `problem+json`.
- **Abstrações na Application.** `GerarTokenUseCase` depende de `IEmissorDeToken` e `IValidadorDeCredenciais`; o emissor
  usa `IRelogio` para a expiração. Emissão e validação de credenciais ficam em `Infrastructure/Seguranca`.
- **Emissão com `JsonWebTokenHandler`, validação com `FastEndpoints.Security`.** O `JwtBearer.CreateToken` do FastEndpoints
  depende do service locator estático da biblioteca (só existe com o host FastEndpoints rodando), o que acoplaria a
  Infrastructure ao runtime da Api e impediria o teste isolado. A Infrastructure emite o token com
  `Microsoft.IdentityModel.JsonWebTokens` (a mesma biblioteca usada pelo FastEndpoints por baixo) e a Api usa
  `FastEndpoints.Security` apenas para configurar a validação JWT Bearer.
- **Comparação de senha por hash em tempo constante.** Compara-se o SHA-256 do usuário e da senha informados com o dos
  configurados usando `CryptographicOperations.FixedTimeEquals`. Comparar os bytes diretos vazaria o tamanho da senha,
  pois a função retorna imediatamente quando os tamanhos diferem.
- **Sem tolerância de relógio.** `TokenValidationParameters.ClockSkew = TimeSpan.Zero`; com o padrão de 5 minutos, um
  token expirado continuaria aceito e o teste de expiração passaria por engano. Emissor, audiência, assinatura e
  expiração são validados.
- **`expiraEm` é duração em segundos** (estilo `expires_in`): `JWT_EXPIRA_MINUTOS × 60`.
- **Falha rápida na inicialização.** As opções são lidas e validadas já no registro de serviços (`OpcoesDeSeguranca.Carregar`),
  antes de migrações e do host subir — mais cedo que `ValidateOnStart` — porque a chave é necessária para configurar a
  validação JWT. Chave ausente ou com menos de 32 caracteres encerra o processo com mensagem clara e código diferente de zero.
- **Testes com configuração JWT própria.** O `ApiFactory` dos testes de integração passa a definir `JWT_*` e `AUTH_*`
  de teste; sem isso todos os testes da fundação deixariam de subir. Um endpoint protegido existente só no assembly de
  testes (`/api/v1/teste-protegido`, fora do Swagger) prova a proteção antes de existirem endpoints de negócio.
- **Swagger.** O `FastEndpoints.Swagger` já declara o esquema `JWTBearerAuth` e o botão Authorize; resta verificar que
  os endpoints protegidos declaram o requisito Bearer e que `auth/token` e `/info` não o declaram.

## Risks / Trade-offs

- [Usuário único em variável de ambiente] → aceitável para uma API de referência; documentado no README como limitação.
- [Chave simétrica compartilhada] → exigido mínimo de 32 caracteres e nenhum valor real no repositório.
- [Endpoint de teste no host de integração] → registrado só no projeto de testes e excluído do Swagger.
- [Esquecer `Group<ApiV1>` num endpoint novo] → teste de arquitetura e teste de rotas registradas acusam.
