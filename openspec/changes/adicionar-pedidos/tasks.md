## 1. Domain

- [ ] 1.1 Enum `StatusPedido`, entidades `Pedido` e `ItemPedido` com cálculo de `subtotal`/`desconto`/`total` (arredondamento afastando de zero); verificar com testes unitários dos limites 499,99, 500,00, 999,99 e 1000,00
- [ ] 1.2 Transições `Pagar()`, `Enviar()` e `Cancelar()` no `Pedido`, lançando `RegraDeNegocioException` quando inválidas; verificar com testes unitários de todas as combinações de status
- [ ] 1.3 Interface `IPedidoRepository` (incluindo `ExisteParaClienteAsync`) e operações de baixa/devolução de estoque em `IProdutoRepository`; verificar compilação e mocks nos testes de casos de uso

## 2. Persistência

- [ ] 2.1 Script `0004_criar_pedidos.sql` com `pedidos`, `itens_pedido` (ids `integer` identity), chaves estrangeiras `integer`, `check` e os quatro índices (ver design.md); verificar no teste de integração que tabelas e índices existem
- [ ] 2.2 `PedidoRepository` (Dapper): inserir pedido com itens, obter com itens, listar com filtros paginados e atualizar status com `versao` (zero linhas → `ConflitoException`); verificar com testes de integração do repositório
- [ ] 2.3 Baixa atômica (`estoque >= @q`) e devolução de estoque no `ProdutoRepository`, em ordem de `produto_id`; verificar com teste de integração de estoque insuficiente

## 3. Casos de uso

- [ ] 3.1 `CriarPedidoUseCase` (cliente/produto inexistente, preço vigente, reserva, rollback, `criadoEm` via `IRelogio`); verificar com `CriarPedidoUseCaseTests`, incluindo rollback sem commit quando falta estoque no segundo item
- [ ] 3.2 `PagarPedidoUseCase`, `EnviarPedidoUseCase` e `CancelarPedidoUseCase` (cancelamento devolve estoque na mesma transação); verificar com `<Nome>UseCaseTests`
- [ ] 3.3 `ObterPedidoUseCase` e `ListarPedidosUseCase`; verificar com `<Nome>UseCaseTests`
- [ ] 3.4 `ExcluirClienteUseCase` passa a rejeitar cliente com pedidos (`ConflitoException`) e a FK violada é traduzida em 409; verificar atualizando `ExcluirClienteUseCaseTests`

## 4. Api

- [ ] 4.1 Seis endpoints em `Endpoints/Pedidos/` com `Validator` (itens não vazios, sem produto repetido, quantidade >= 1, `de <= ate`, paginação) e `Summary`; verificar com testes unitários dos validators e no Swagger

## 5. Testes de integração

- [ ] 5.1 Fluxo ponta a ponta: criar cliente e produto, criar pedido, pagar e enviar, conferindo status, totais e estoque; verificar com `dotnet test`
- [ ] 5.2 Estoque insuficiente (`422` sem alterações), cancelamento devolvendo estoque, transições inválidas (`422`), preço histórico preservado e filtros da listagem; verificar com `dotnet test`
- [ ] 5.3 Concorrência: última unidade disputada (uma vence, outra `422`) e pagamento duplo (`409` ou `422`); verificar executando a suíte várias vezes sem intermitência
- [ ] 5.4 `DELETE /clientes/{id}` com pedidos responde `409`; verificar com `dotnet test`

## 6. Documentação

- [ ] 6.1 Exemplos do fluxo de pedido no `requests.http` e regras de desconto/status no README; verificar executando os exemplos
