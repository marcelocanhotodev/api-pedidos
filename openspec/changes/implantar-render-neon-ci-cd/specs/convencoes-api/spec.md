## MODIFIED Requirements

### Requirement: Informações da API
O sistema DEVE (SHALL) expor `GET /info`, sem autenticação, implementado pelo caso de uso `ObterInformacoesUseCase`, retornando `nome`, `versao` e `ambiente` da API, e o endpoint DEVE (SHALL) aparecer no Swagger. Quando a plataforma informar o commit implantado (variável `RENDER_GIT_COMMIT` ou `VERSAO_COMMIT`), a `versao` DEVE (SHALL) incluí-lo no formato `<versão>+<7 primeiros caracteres do commit>`.

#### Scenario: Consultar informações
- **WHEN** um cliente chama `GET /info`
- **THEN** a API responde `200` com `nome`, `versao` e `ambiente`

#### Scenario: Visível no Swagger
- **WHEN** o documento OpenAPI é gerado
- **THEN** ele contém a rota `/info` com resumo e modelo de resposta

#### Scenario: Commit implantado na versão
- **WHEN** a API roda com `RENDER_GIT_COMMIT=0123456789abcdef`
- **THEN** `GET /info` informa `versao` igual a `1.0.0+0123456`

#### Scenario: Sem commit informado
- **WHEN** a API roda sem `RENDER_GIT_COMMIT` nem `VERSAO_COMMIT`
- **THEN** `GET /info` informa apenas a versão do assembly
