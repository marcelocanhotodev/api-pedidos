# Design

## Contexto
Projeto novo. O banco é PostgreSQL; o acesso a dados fica atrás de interfaces de repositório, de modo que
trocar de provedor (por exemplo Oracle) exija mudar apenas a camada Infrastructure e os scripts SQL.
Esta change entrega só a base; os recursos de negócio chegam em fatias posteriores (ver proposal.md).

## Objetivos / Não-objetivos
- Objetivos: camadas claras, SQL explícito, execução reproduzível via Docker e uma base onde cada fatia
  acrescenta código, migração e testes sem alterar a infraestrutura comum.
- Não-objetivos: multi-tenant, mensageria, cache, front-end, pagamento real, YAML do pipeline Azure.
  Autenticação, entidades e tabelas de negócio ficam nas fatias seguintes.

## Decisões
- **Somente Dapper (sem EF).** SQL explícito e desempenho previsível. Trade-off: mais código de mapeamento e
  nenhum *change tracking*; aceito em troca de controle total e simplicidade.
- **DbUp para migrações.** Scripts `.sql` embutidos como *embedded resources*, executados em ordem alfabética,
  somente para frente (sem *rollback* automático), controle na tabela `schemaversions`.
- **Uma migração por fatia.** A fundação entrega apenas o executor; cada fatia traz seus próprios scripts com
  numeração reservada: `0001_criar_clientes`, `0002_criar_produtos`, `0003_criar_pedidos`. A numeração fixa
  evita conflito quando clientes e produtos são implementados em paralelo.
  Alternativa descartada: um único `0001_criar_tabelas.sql` — impede entregar as fatias separadamente, já que
  scripts aplicados não podem ser editados.
- **FastEndpoints (REPR).** Um endpoint por classe, com `Request`, `Response`, `Validator` e `Summary` próprios.
  Endpoints não contêm regra de negócio; apenas traduzem HTTP e chamam um caso de uso.
- **Clean Architecture.** Casos de uso na Application, dependendo apenas de interfaces. Infrastructure implementa essas interfaces.
- **Um caso de uso por endpoint** (`IUseCase<TEntrada, TSaida>`, método `ExecutarAsync`). Cada classe tem uma única
  responsabilidade e é testada isoladamente com mocks. Trade-off: mais arquivos (3 por caso de uso: UseCase, Entrada, Saida),
  aceito em troca de testes simples e de endpoints triviais. Casos de uso não chamam uns aos outros; regra comum vai para o Domain.
- **Convenção em vez de catálogo.** O teste de arquitetura valida a convenção (nome, pasta, `Entrada`/`Saida`,
  um endpoint por caso de uso) e não uma lista fixa; cada fatia declara seus casos de uso na própria spec.
  Alternativa descartada: tabela com os 18 casos de uso alterada (MODIFIED) a cada fatia.
- **Repository Pattern + Unit of Work.** Um repositório por agregado, com interface no Domain.
  `IUnitOfWork` controla a transação sobre uma conexão compartilhada (`IDbSession`, escopo por requisição).
- **Exceções de domínio** (`NaoEncontradoException`, `ConflitoException`, `RegraDeNegocioException`)
  convertidas em `ProblemDetails` por um único tratador global.
- **Mapeamento snake_case ↔ PascalCase** com `DefaultTypeMap.MatchNamesWithUnderscores = true`.

## Estrutura da solução
```
src/
├── Pedidos.Domain/          Entidades/  Enums/  Regras/  Excecoes/  Repositorios/ (interfaces)
├── Pedidos.Application/     CasosDeUso/<Area>/<NomeDoCasoDeUso>/{UseCase,Entrada,Saida}.cs  Abstracoes/ (IUseCase, IUnitOfWork, IRelogio)
├── Pedidos.Infrastructure/  Dados/(DbSession, UnitOfWork, Repositorios/, Consultas/)  Migracoes/Scripts/*.sql
└── Pedidos.Api/             Endpoints/<Area>/  Configuracao/  Program.cs
tests/
├── Pedidos.UnitTests/       CasosDeUso/<Area>/<Nome>UseCaseTests.cs  Validators/  Arquitetura/
└── Pedidos.IntegrationTests/
```

## Convenções de banco (para todas as fatias)
`snake_case`, tabelas no plural, chaves `uuid`, datas `timestamptz` em UTC, dinheiro `numeric(12,2)`.

## Exemplo de contrato (ilustrativo)
```csharp
public interface IUseCase<in TEntrada, TSaida>
{
    Task<TSaida> ExecutarAsync(TEntrada entrada, CancellationToken ct);
}
```

## Riscos / Trade-offs
- Sem ORM, SQL e mapeamento são responsabilidade do time → mitigado por testes de integração com PostgreSQL real.
- DbUp não faz *rollback* → correções são novos scripts; scripts já aplicados nunca são editados.
- Fundação sem endpoint de negócio → parte das convenções (validação, versionamento) só é exercitada a partir
  da primeira fatia com endpoint; o tratador de erros é coberto aqui por testes do mapeamento de exceções.

## Questões em aberto
- Especificar o pipeline do Azure DevOps (build, testes, push da imagem) em uma change futura? (proposta: sim)
