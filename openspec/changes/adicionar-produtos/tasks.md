## 1. Domain e persistência

- [ ] 1.1 Entidade `Produto` (regra de estoque não negativo) e interface `IProdutoRepository` no Domain; verificar com testes unitários da entidade
- [ ] 1.2 Script `0002_criar_produtos.sql`: `id uuid pk`, `sku varchar(50) not null unique`, `nome varchar(150) not null`, `preco numeric(12,2) not null check (preco >= 0)`, `estoque int not null check (estoque >= 0)`, `criado_em timestamptz not null`; verificar que a migração aplica em banco vazio no teste de integração
- [ ] 1.3 `ProdutoRepository` (Dapper) com busca paginada, ajuste atômico `UPDATE ... SET estoque = estoque + @delta WHERE id = @id AND estoque + @delta >= 0` e tradução de SKU duplicado em `ConflitoException`; verificar com testes de integração do repositório

## 2. Casos de uso

- [ ] 2.1 `CriarProdutoUseCase` e `AtualizarProdutoUseCase`; verificar com `<Nome>UseCaseTests` (SKU duplicado, inexistente)
- [ ] 2.2 `ObterProdutoUseCase` e `ListarProdutosUseCase`; verificar com `<Nome>UseCaseTests`
- [ ] 2.3 `AjustarEstoqueProdutoUseCase` (resultado negativo → regra de negócio); verificar com `AjustarEstoqueProdutoUseCaseTests`

## 3. Api

- [ ] 3.1 Cinco endpoints em `Endpoints/Produtos/` com `Validator` (sku, nome, preço com duas casas, estoque, paginação) e `Summary`; verificar com testes unitários dos validators e no Swagger

## 4. Testes de integração

- [ ] 4.1 Fluxo com token: criar (`201`), SKU duplicado (`409`), preço negativo (`400`), obter, busca paginada, atualizar preço, ajustar estoque (`+10` → 15, `-10` → `422` com estoque inalterado) e `check` de estoque no banco; verificar com `dotnet test`

## 5. Documentação

- [ ] 5.1 Exemplos de produtos no `requests.http`; verificar executando os exemplos
