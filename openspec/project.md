# Contexto do Projeto

## Propósito
API REST de referência para gerenciar clientes, produtos e pedidos. Demonstra Clean Architecture,
Repository Pattern, SOLID, acesso a dados com Dapper, migrações com DbUp, testes automatizados
e execução totalmente containerizada.

## Stack
- C# 14 / .NET 10 (LTS), ASP.NET Core
- **FastEndpoints** (padrão REPR: uma classe por endpoint), `FastEndpoints.Swagger`, `FastEndpoints.Security`
- Validação com os validators do FastEndpoints (FluentValidation)
- **Dapper** + Npgsql (`NpgsqlDataSource`) para todo o acesso a dados
- **DbUp** (`dbup-postgresql`) para migrações versionadas em scripts `.sql`
- PostgreSQL 16
- Autenticação JWT Bearer
- Serilog (logs JSON no stdout)
- Testes: xUnit, Moq, Testcontainers (PostgreSQL), `WebApplicationFactory`
- Docker (build multi-stage) e Docker Compose
- Git no GitHub; CI/CD com GitHub Actions (`.github/workflows`)
- Produção gratuita: API no Render (Docker, `render.yaml`) e PostgreSQL no Neon

## Arquitetura (Clean Architecture)
A regra de dependência aponta sempre para dentro:

```
Pedidos.Api  ──►  Pedidos.Application  ──►  Pedidos.Domain
      │                    ▲
      └──► Pedidos.Infrastructure (implementa as abstrações) ──► Application, Domain
```

- **Domain**: entidades, enums, regras de negócio, exceções de domínio e interfaces de repositório.
- **Application**: um caso de uso por endpoint (`IUseCase<TEntrada, TSaida>`), entradas/saídas próprias, `IUnitOfWork`, `IRelatorioQueries`, abstrações (relógio, hash, token).
- **Infrastructure**: repositórios Dapper, `IDbSession`/UnitOfWork, DbUp, geração de JWT, consultas de relatório.
- **Api**: endpoints FastEndpoints, validators, configuração, middlewares, composição de dependências.
- Testes: `Pedidos.UnitTests` e `Pedidos.IntegrationTests`.

## Convenções
- Nomes de domínio, tabelas, colunas, rotas e campos JSON em **português** (ex.: `clientes`, `precoUnitario`).
- Código: SOLID, Clean Code, `nullable` habilitado, async de ponta a ponta (sem `.Result`/`.Wait()`).
- Banco: `snake_case`, tabelas no plural, chave primária `id integer` autoincremental (`GENERATED ALWAYS AS IDENTITY`), relações por `<entidade>_id integer` com chave estrangeira, datas em UTC (`timestamptz`).
- API: o `id` exposto em rotas e JSON é o mesmo inteiro da chave primária.
- JSON em camelCase; rotas em kebab-case, prefixo `/api/v1`.
- Git: trunk-based, branches curtas `feature/*`, Conventional Commits, PRs pequenos.
- Configuração somente por variáveis de ambiente; nenhum segredo no repositório.

## Contexto de Domínio
Um cliente faz pedidos compostos por itens; cada item referencia um produto com estoque.
Fluxo de status: `Pendente → Pago → Enviado`; `Pendente` ou `Pago` podem virar `Cancelado`.

## Restrições
- Roda inteiramente com `docker compose up`; não exige SDK .NET nem PostgreSQL locais.
- Um serviço, um banco; sem mensageria nem cache nesta versão.
- **Proibido** usar Entity Framework ou outro ORM completo.
