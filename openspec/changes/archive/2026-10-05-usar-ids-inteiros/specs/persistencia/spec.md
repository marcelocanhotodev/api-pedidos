## ADDED Requirements

### Requirement: Chaves Primárias Inteiras
Toda tabela de negócio DEVE (SHALL) ter a chave primária `id` do tipo `integer`, gerada pelo próprio banco como identidade (`GENERATED ALWAYS AS IDENTITY`), e toda relação entre tabelas DEVE (SHALL) ser uma chave estrangeira `integer` que referencia o `id` da tabela relacionada, nomeada `<entidade>_id`. A aplicação NÃO DEVE (SHALL NOT) gerar nem informar o valor do `id` ao inserir; o valor gerado DEVE (SHALL) ser devolvido pelo próprio `INSERT`.

#### Scenario: Id gerado pelo banco
- **WHEN** dois registros são inseridos em sequência numa tabela vazia
- **THEN** recebem ids inteiros crescentes gerados pelo banco, e o id de cada um é conhecido pela aplicação sem nova consulta

#### Scenario: Id informado pela aplicação recusado
- **WHEN** um `INSERT` tenta informar explicitamente o valor da coluna `id`
- **THEN** o banco rejeita o comando

#### Scenario: Relação por chave estrangeira inteira
- **WHEN** o esquema é inspecionado
- **THEN** toda coluna `<entidade>_id` é `integer` com chave estrangeira para `<entidades>.id`

#### Scenario: Conversão sem perda de dados
- **WHEN** a migração que troca uma chave `uuid` por `integer` é aplicada sobre uma tabela com registros
- **THEN** todos os registros permanecem, cada um com um id inteiro único, e as demais colunas inalteradas
