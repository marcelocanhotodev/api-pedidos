# Change: Adicionar Fundação

## Why
Ainda não existe aplicação. Antes de qualquer recurso de negócio precisamos de um esqueleto executável — camadas,
acesso a dados, migrações, convenções HTTP, containers e portões de qualidade — para que cada fatia seguinte
(segurança, clientes, produtos, pedidos, relatórios) entre como um PR pequeno sobre uma base já testada.

## What Changes
- Solução .NET 10 com as camadas Domain, Application, Infrastructure e Api, mais projetos de testes.
- Contrato `IUseCase<TEntrada, TSaida>` com registro automático por varredura (nenhum caso de uso de negócio ainda).
- Dapper + `NpgsqlDataSource`, `IDbSession`/`IUnitOfWork` com escopo por requisição.
- Executor DbUp no startup, com espera pelo banco; ainda sem scripts de tabelas (cada fatia traz o seu).
- Tratador global de erros `ProblemDetails`, modelo de paginação compartilhado, Swagger, health checks e logs JSON.
- Dockerfile multi-stage e Docker Compose com PostgreSQL.
- Teste de arquitetura e portões de qualidade (nullable, warnings como erro).

Fatias seguintes (changes separadas, arquivadas nesta ordem): `adicionar-seguranca` → `adicionar-clientes` e
`adicionar-produtos` (independentes entre si) → `adicionar-pedidos` → `adicionar-relatorios`.

## Capabilities

### New Capabilities
- `arquitetura`: camadas, regra de dependência, Repository Pattern, um caso de uso por endpoint (convenção, sem catálogo fixo)
- `persistencia`: Dapper, Unit of Work, migrações DbUp e espera pelo banco
- `convencoes-api`: formato de erros `ProblemDetails`, paginação, documentação Swagger, health checks e logs estruturados
- `plataforma`: Dockerfile, Docker Compose, configuração por variáveis de ambiente
- `qualidade`: testes unitários por caso de uso, testes de integração, teste de arquitetura, rastreabilidade e portões

### Modified Capabilities
_Nenhuma (não há specs arquivadas)._

## Impact
- Código novo: `src/` (4 projetos), `tests/` (2 projetos), `Dockerfile`, `docker-compose.yml`, `.env.example`, `README.md`.
- Nenhum endpoint de negócio; apenas `/health/live`, `/health/ready` e `/swagger`.
- Breaking changes: nenhum (projeto novo).
