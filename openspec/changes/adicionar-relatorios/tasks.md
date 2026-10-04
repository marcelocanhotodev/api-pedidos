## 1. Application

- [ ] 1.1 Abstração `IRelatorioQueries` e DTO de linha do ranking em `Application/Abstracoes`; verificar compilação e uso em mock
- [ ] 1.2 `ObterRankingClientesUseCase` com `Entrada`/`Saida` próprias e `top` padrão 10; verificar com `ObterRankingClientesUseCaseTests`

## 2. Infrastructure

- [ ] 2.1 `RelatorioQueries` (Dapper) com CTE, `ROW_NUMBER` para último pedido e posição, filtro `Pago`/`Enviado` e `LIMIT @top`, tudo parametrizado; verificar com teste de integração da consulta

## 3. Api

- [ ] 3.1 Endpoint `GET /api/v1/relatorios/ranking-clientes` com `Validator` (`de`/`ate` obrigatórios, `de <= ate`, `top` 1–100) e `Summary`; verificar com testes unitários do validator e no Swagger

## 4. Testes de integração

- [ ] 4.1 Massa com clientes A (900,00) e B (1200,00), cliente só com pendentes/cancelados, empate no total e 15 clientes qualificados; verificar ordem, exclusões, desempate sem lacunas, limite padrão de 10 e campos do último pedido
- [ ] 4.2 Parâmetros inválidos (`400`), parâmetro com fragmento SQL tratado como dado e existência dos índices em `pedidos`; verificar com `dotnet test`

## 5. Documentação

- [ ] 5.1 Exemplo do relatório no `requests.http` e explicação da consulta (CQRS leve) no README; verificar executando o exemplo
