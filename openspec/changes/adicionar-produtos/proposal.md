# Change: Adicionar Produtos

## Why
Pedidos precisam de produtos com preço e estoque. Entregar o catálogo e o ajuste de estoque como fatia própria
deixa a fatia de pedidos concentrada só nas regras do pedido.

## What Changes
- CRUD de produtos em `/api/v1/produtos` (sem exclusão), com listagem paginada e busca.
- Ajuste manual de estoque por `PATCH /api/v1/produtos/{id}/estoque`.
- Casos de uso `CriarProdutoUseCase`, `ObterProdutoUseCase`, `ListarProdutosUseCase`, `AtualizarProdutoUseCase`, `AjustarEstoqueProdutoUseCase`.
- Entidade `Produto`, `IProdutoRepository` e migração `0002_criar_produtos.sql`.

Depende de: `adicionar-fundacao` e `adicionar-seguranca`. Independente de `adicionar-clientes`.

## Capabilities

### New Capabilities
- `produtos`: cadastro, consulta, atualização de produtos e ajuste de estoque

### Modified Capabilities
_Nenhuma._

## Impact
- Código: `Domain/Entidades/Produto`, `CasosDeUso/Produtos/*`, `Infrastructure/Dados/Repositorios/ProdutoRepository`, `Api/Endpoints/Produtos/*`.
- Banco: tabela `produtos` (script `0002`).
