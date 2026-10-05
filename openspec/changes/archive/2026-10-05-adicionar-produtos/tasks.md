## 1. Domain

- [x] 1.1 Entidade `Produto` com `Criar(sku, nome, preco, estoque, agora)`, `Atualizar(nome, preco)`, `Restaurar(...)` e `ValidarDelta(delta)`: SKU com `Trim` + maiúsculas e só `A-Z 0-9 - _ .` (1–50), nome com `Trim` (1–150), preço `0..9999999999.99` com até 2 casas e normalizado para exatamente 2 casas (`4.9` → `4.90`), estoque `0..1000000`, delta `!= 0` e `|delta| <= 1000000`, `RegraDeNegocioException` em violação, `Id` `int` (0 até ser persistido) e constantes/checagens públicas; verificar com testes unitários da entidade (normalização, cada limite, 2 vs 3 casas, escala do preço, delta)
- [x] 1.2 Interface `IProdutoRepository` com ids `int` (obter, existe SKU, inserir devolvendo o produto persistido via `RETURNING id`, atualizar nome/preço, ajustar estoque atômico retornando o produto, listar paginado com busca); verificar compilação e uso em mocks

## 2. Persistência

- [x] 2.1 Script `0003_criar_produtos.sql` com a tabela (`id integer generated always as identity`), os `check` e o índice único `ux_produtos_sku` (ver design.md); verificar no teste de integração que a migração aplica em banco vazio e que índice e `check` existem
- [x] 2.2 `ProdutoRepository` (Dapper, via `IDbSession`): listagem com `count(*) OVER()`, `ORDER BY nome, id` e `ILIKE ... ESCAPE '\'` em nome ou SKU; `UPDATE` do `PUT` gravando só `nome` e `preco`; tradução de `23505` em `ux_produtos_sku` para `ConflitoException`; verificar com testes de integração do repositório (busca literal, ordem estável, conflito no índice)
- [x] 2.3 Ajuste atômico `UPDATE ... SET estoque = estoque + @delta WHERE id = @id AND estoque::bigint + @delta BETWEEN 0 AND 1000000 RETURNING ...`, distinguindo inexistente (`NaoEncontradoException`) de fora do intervalo (`RegraDeNegocioException`) quando nenhuma linha é afetada; verificar com testes de integração (limite inferior, superior, inexistente e 10 ajustes simultâneos somando exato)

## 3. Casos de uso

- [x] 3.1 `CriarProdutoUseCase` e `AtualizarProdutoUseCase` com `Saida` próprias (SKU existente → `ConflitoException`, inexistente → `NaoEncontradoException`, `criadoEm` via `IRelogio`, `EmTransacaoAsync`); verificar com `<Nome>UseCaseTests`
- [x] 3.2 `ObterProdutoUseCase` e `ListarProdutosUseCase` (`ListarProdutosSaida` envolvendo `PaginaResultado<ProdutoResumo>`); verificar com `<Nome>UseCaseTests`
- [x] 3.3 `AjustarEstoqueProdutoUseCase` (valida o delta pela entidade antes de abrir transação, aplica pelo repositório, devolve `AjustarEstoqueProdutoSaida` com o estoque resultante); verificar com `AjustarEstoqueProdutoUseCaseTests`

## 4. Api

- [x] 4.1 Cinco endpoints em `Endpoints/Produtos/` com `Group<ApiV1>()`, `Validator` usando as checagens da entidade (sku, nome, preço, estoque, delta, paginação, busca), `Summary` e `CreatedAtAsync<ObterProdutoEndpoint>` na criação; `PUT` sem `sku`/`estoque` no request; verificar com testes unitários dos validators, teste de rotas registradas e Swagger

## 5. Testes de integração

- [x] 5.1 Fluxo com token: criar (`201` + `Location`), SKU normalizado, SKU duplicado com outra caixa (`409`), SKU inválido, preço negativo, com 3 casas e acima do limite (`400`), estoque inicial acima do limite (`400`), obter/inexistente, busca por SKU em qualquer caixa, `4.9` devolvido como `4.90` no `POST` e no `GET`, busca com `%`, nomes repetidos entre páginas, `PUT` de preço mantendo SKU e estoque, `PUT` inexistente (`404`), ajustes (`+10` → 15, `-10` → `422`, acima de 1.000.000 → `422`, `delta = 0` → `400`, inexistente → `404`) e acesso sem token (`401`); verificar com `dotnet test`
- [x] 5.2 Concorrência: 10 `PATCH` simultâneos de `delta = 1` → todos `200` e estoque 10; 1 `PUT` junto com 10 `PATCH` → nome/preço do `PUT` e estoque 10; dois `POST` simultâneos com o mesmo SKU → um `201` e um `409`; verificar com `dotnet test` repetido sem intermitência

## 6. Documentação

- [x] 6.1 Exemplos de produtos no `requests.http`, endpoints e decisões (limites, SKU, ajuste atômico) no README; verificar executando os exemplos contra o container
