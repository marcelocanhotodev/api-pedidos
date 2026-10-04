# Change: Adicionar Clientes

## Why
Clientes são pré-requisito para pedidos e o primeiro recurso de negócio da API. Um CRUD simples valida a fundação
de ponta a ponta (endpoint → caso de uso → repositório Dapper → migração) antes do domínio mais rico.

## What Changes
- CRUD de clientes em `/api/v1/clientes` com listagem paginada e busca.
- Casos de uso `CriarClienteUseCase`, `ObterClienteUseCase`, `ListarClientesUseCase`, `AtualizarClienteUseCase`, `ExcluirClienteUseCase`.
- Entidade `Cliente`, `IClienteRepository` e migração `0001_criar_clientes.sql`.
- A exclusão não verifica pedidos nesta fatia (ainda não existem); a restrição chega em `adicionar-pedidos`.

Depende de: `adicionar-fundacao` e `adicionar-seguranca`. Independente de `adicionar-produtos`.

## Capabilities

### New Capabilities
- `clientes`: cadastro, consulta, atualização e exclusão de clientes

### Modified Capabilities
_Nenhuma._

## Impact
- Código: `Domain/Entidades/Cliente`, `CasosDeUso/Clientes/*`, `Infrastructure/Dados/Repositorios/ClienteRepository`, `Api/Endpoints/Clientes/*`.
- Banco: tabela `clientes` (script `0001`).
