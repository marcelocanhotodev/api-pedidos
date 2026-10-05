# Change: Adicionar Produtos

## Why
Pedidos precisam de produtos com preço e estoque. Entregar o catálogo e o ajuste de estoque como fatia própria
deixa a fatia de pedidos concentrada só nas regras do pedido.

## What Changes
- CRUD de produtos em `/api/v1/produtos` (sem exclusão), com listagem paginada e busca.
- Ajuste manual de estoque por `PATCH /api/v1/produtos/{id}/estoque`.
- Casos de uso `CriarProdutoUseCase`, `ObterProdutoUseCase`, `ListarProdutosUseCase`, `AtualizarProdutoUseCase`, `AjustarEstoqueProdutoUseCase`.
- Entidade `Produto`, `IProdutoRepository` e migração `0003_criar_produtos.sql` (a `0002` é a conversão de ids de `usar-ids-inteiros`).

Depende de: `adicionar-fundacao`, `adicionar-seguranca` e `usar-ids-inteiros`.

## Capabilities

### New Capabilities
- `produtos`: cadastro, consulta, atualização de produtos e ajuste de estoque

### Modified Capabilities
_Nenhuma._

## Impact
- Código: `Domain/Entidades/Produto`, `CasosDeUso/Produtos/*`, `Infrastructure/Dados/Repositorios/ProdutoRepository`, `Api/Endpoints/Produtos/*`.
- Banco: tabela `produtos` com `id integer` autoincremental (script `0003`).
