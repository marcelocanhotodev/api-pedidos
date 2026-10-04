# API de Pedidos

API REST de referência em **.NET 10** com **FastEndpoints**, **Dapper** (sem EF), **DbUp**, **Clean Architecture** +
**Repository Pattern**, **PostgreSQL 16** e execução totalmente em **Docker**. O projeto é guiado por especificação
(OpenSpec) e entregue em fatias pequenas.

## Como rodar

Pré-requisito: Docker (não é preciso SDK .NET nem PostgreSQL locais).

```bash
cp .env.example .env
docker compose up --build
```

| URL | O que é |
|-----|---------|
| http://localhost:8080/swagger | Swagger (documentação interativa) |
| http://localhost:8080/info | Nome, versão e ambiente da API |
| http://localhost:8080/health/live | Processo vivo (`200`) |
| http://localhost:8080/health/ready | Banco acessível (`200`) ou não (`503`) |
| http://localhost:8080/api/v1/auth/token | Emissão de token JWT (`POST`) |

Exemplos prontos em [`requests.http`](requests.http).

- `docker compose down` mantém os dados (volume `pedidos-dados`).
- `docker compose down -v` apaga o volume; a próxima subida cria um banco vazio e reaplica as migrações.

## Autenticação

Todo endpoint sob `/api/v1` exige `Authorization: Bearer <token>`, exceto a própria emissão de token.
`/info`, `/health/*` e `/swagger` são públicos.

```bash
curl -s -X POST http://localhost:8080/api/v1/auth/token \
  -H 'Content-Type: application/json' \
  -d '{"usuario":"admin","senha":"admin_dev"}'
# {"accessToken":"eyJ...","expiraEm":3600}
```

- `expiraEm` é a validade em segundos (`JWT_EXPIRA_MINUTOS × 60`); não há tolerância de relógio para tokens expirados.
- Credenciais erradas respondem `401` em `application/problem+json`; requisições sem token ou com token inválido também.
- No Swagger, use **Authorize** e cole o `accessToken`.
- **Limitação:** existe um único usuário, definido por `AUTH_USUARIO`/`AUTH_SENHA`. Não há cadastro de usuários, papéis nem refresh token.

## Como testar

Com o SDK .NET 10 instalado:

```bash
dotnet test Pedidos.slnx
```

Sem SDK local, usando o container do SDK (os testes de integração sobem PostgreSQL com Testcontainers,
por isso o socket do Docker é montado):

```bash
docker run --rm -v "$PWD:/src" -w /src \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal \
  mcr.microsoft.com/dotnet/sdk:10.0 dotnet test Pedidos.slnx
```

- `Pedidos.UnitTests`: casos de uso, validators, mapeamento de erros, paginação e **testes de arquitetura**. Não usa Docker nem rede.
- `Pedidos.IntegrationTests`: API real (`WebApplicationFactory`) contra PostgreSQL real (Testcontainers), com migrações DbUp.

## Variáveis de ambiente

Toda a configuração vem de variáveis de ambiente, documentadas em [`.env.example`](.env.example) com valores
**apenas de desenvolvimento**. O arquivo `.env` é ignorado pelo Git e pelo contexto de build do Docker.

| Variável | Uso |
|----------|-----|
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | Criação do banco no container `postgres` |
| `ConnectionStrings__Padrao` | Conexão da API com o PostgreSQL |
| `APLICAR_MIGRACOES` | `true` aplica as migrações DbUp ao iniciar; `false` não altera o esquema |
| `ASPNETCORE_ENVIRONMENT` | `Development` ou `Production` |
| `JWT_CHAVE` | Chave de assinatura HS256, **mínimo 32 caracteres**; ausente ou curta, a API não inicia |
| `JWT_EMISSOR`, `JWT_AUDIENCIA` | Emissor (`iss`) e audiência (`aud`) emitidos e exigidos nos tokens |
| `JWT_EXPIRA_MINUTOS` | Validade do token em minutos |
| `AUTH_USUARIO`, `AUTH_SENHA` | Único usuário autorizado a emitir tokens |

## Estrutura

```
src/
├── Pedidos.Domain/          entidades, regras e exceções de domínio, interfaces de repositório
├── Pedidos.Application/     um caso de uso por endpoint (CasosDeUso/<Area>/<Nome>/), abstrações, paginação
├── Pedidos.Infrastructure/  Dapper (DbSession, UnitOfWork), DbUp (Migracoes/Scripts/*.sql), health check do banco
└── Pedidos.Api/             endpoints FastEndpoints, tratamento de erros, Swagger, logs, Program.cs
tests/
├── Pedidos.UnitTests/
└── Pedidos.IntegrationTests/
openspec/                    especificações e changes (fonte da verdade do comportamento)
```

Regra de dependência: `Api → Application → Domain` e `Infrastructure → Application, Domain`.
Os testes de arquitetura quebram o build se uma camada interna referenciar uma externa, se aparecer Entity Framework,
se um endpoint receber repositório/`IUnitOfWork`/conexão ou se um caso de uso fugir da convenção de nomes e pastas.

## Decisões e trade-offs

- **Somente Dapper, sem ORM**: SQL explícito e previsível; em troca, mais código de mapeamento, coberto por testes de integração.
- **DbUp, somente para frente**: scripts `.sql` embutidos, um por fatia, com numeração reservada
  (`0001_criar_clientes`, `0002_criar_produtos`, `0003_criar_pedidos`). Scripts aplicados nunca são editados; correções viram scripts novos.
  A fundação traz apenas `0000_linha_de_base.sql`, que garante que o pipeline de migrações funcione desde o início.
- **Um caso de uso por endpoint** (`IUseCase<TEntrada, TSaida>`): endpoints só traduzem HTTP; a regra fica testável com mocks.
  Os casos de uso são registrados automaticamente por varredura do assembly da Application.
- **Unit of Work por requisição**: uma conexão e, quando necessário, uma transação compartilhada pelos repositórios.
  Uma transação não confirmada é desfeita ao fim da requisição.
- **Datas em UTC**: colunas `timestamptz` são lidas e gravadas como `DateTimeOffset` em UTC (type handler do Dapper).
- **Erros em `application/problem+json`**: não encontrado → `404`, conflito → `409`, regra de negócio → `422`,
  validação → `400` com `errors` por campo, falha inesperada → `500` genérico com `traceId`, sem stack trace.
- **Logs JSON no stdout** (Serilog): cada requisição registra método, caminho, status, tempo e `TraceId`.
- **`GET /info`**: o FastEndpoints não inicia sem nenhum endpoint, e a fundação não tem endpoints de negócio. A rota de
  informações mantém a fundação executável e com Swagger, e já segue o padrão endpoint → caso de uso.
- **Endpoints de negócio no grupo `ApiV1`** (prefixo `/api/v1`), protegidos por padrão; `/info` fica fora do grupo.
  Testes sobre as rotas registradas garantem que nenhuma rota `/api/v1` além do token é anônima e que todo endpoint de negócio está no grupo.
- **JWT**: emitido com `Microsoft.IdentityModel.JsonWebTokens` na Infrastructure e validado pelo `FastEndpoints.Security` na Api;
  credenciais comparadas pelo SHA-256 em tempo constante.
- **Espera pelo banco**: a API tenta conectar por até 30 s antes de migrar e encerra com código diferente de zero se o banco não responder.

## Fatias (changes OpenSpec)

Cada fatia é uma change em `openspec/changes/`, implementada em uma branch curta e arquivada nesta ordem:

```
adicionar-fundacao --> adicionar-seguranca --> adicionar-clientes --+--> adicionar-pedidos --> adicionar-relatorios
                                               adicionar-produtos --+
```

`adicionar-clientes` e `adicionar-produtos` são independentes entre si.

```bash
openspec list                                  # changes e progresso
openspec validate adicionar-fundacao --strict  # valida a change
openspec archive adicionar-fundacao --yes      # move os requisitos para openspec/specs/
```

O texto das specs está em pt-BR. Por exigência do validador do OpenSpec, os títulos estruturais
(`## ADDED Requirements`, `### Requirement:`, `#### Scenario:`) e os marcadores **WHEN/THEN** ficam em inglês,
e cada requisito traz `(SHALL)` ao lado de **DEVE**.

## Cenários verificados manualmente

Cenários da fundação que dependem do Docker em execução e não têm teste automatizado:

| Spec | Cenário | Como verificar |
|------|---------|----------------|
| plataforma | Build a partir de clone limpo | `docker build .` conclui; a imagem final não tem `/usr/share/dotnet/sdk` nem código-fonte |
| plataforma | Execução sem root | `docker compose exec api id -un` → `app` |
| plataforma | Subida com um comando | `docker compose up --build` e `curl localhost:8080/health/ready` → `200` |
| plataforma | Persistência de dados | dado gravado continua após `docker compose down` + `up` |
| plataforma | Reset limpo | após `docker compose down -v`, a subida reaplica as migrações em banco vazio |
| plataforma | Arquivo de exemplo | a stack sobe com `.env` copiado de `.env.example` |
| plataforma | Contexto de build enxuto | `bin/`, `obj/`, `.env` e `tests/` ficam fora do contexto (`.dockerignore`) |
| persistencia | Banco ainda subindo | API iniciada antes do PostgreSQL registra "Banco disponível após N tentativas" |
| persistencia | Banco indisponível | `docker compose stop postgres && docker compose run --rm --no-deps api` encerra com código `1` após 30 s |
| convencoes-api | Requisição registrada | `docker compose logs api` mostra JSON com `RequestMethod`, `RequestPath`, `StatusCode`, `Elapsed` e `TraceId` |
| convencoes-api | Swagger disponível | `/swagger` carrega e lista `/info` |
| seguranca | Chave ausente / Chave curta (processo) | `docker compose run --rm --no-deps -e JWT_CHAVE= api` encerra com código `1` e mensagem clara (também coberto por teste de integração) |
| qualidade | Warning quebra o build | um warning proposital (ex.: variável não usada) faz `dotnet build` falhar |
