# persistencia Specification

## Purpose

Define como a aplicação acessa o PostgreSQL (Dapper, Unit of Work) e como o esquema evolui de forma versionada com DbUp, uma migração por fatia.

## Requirements

### Requirement: Acesso a Dados com Dapper
O sistema DEVE (SHALL) usar Dapper com Npgsql (`NpgsqlDataSource`) como único mecanismo de acesso a dados, com SQL parametrizado, e NÃO DEVE (SHALL NOT) utilizar Entity Framework ou outro ORM completo.

#### Scenario: Parâmetros em vez de concatenação
- **WHEN** uma consulta recebe valores vindos do usuário
- **THEN** eles são passados como parâmetros e nunca concatenados ao texto SQL

#### Scenario: Mapeamento de colunas
- **WHEN** uma coluna `criado_em` é lida
- **THEN** ela é mapeada para a propriedade `CriadoEm` sem mapeamento manual por consulta

### Requirement: Unit of Work
O sistema DEVE (SHALL) fornecer `IUnitOfWork` que abre uma transação sobre a conexão do escopo atual, permite `Commit` e `Rollback` e é compartilhada por todos os repositórios da mesma requisição.

#### Scenario: Rollback em falha
- **WHEN** um caso de uso de escrita falha depois de executar parte dos comandos da transação
- **THEN** a transação sofre rollback e nenhuma das alterações é persistida

#### Scenario: Commit único
- **WHEN** o caso de uso termina com sucesso
- **THEN** todas as alterações feitas pelos repositórios na requisição são confirmadas juntas

### Requirement: Migrações com DbUp
O sistema DEVE (SHALL) gerenciar o esquema do banco com DbUp, usando scripts `.sql` embutidos como *embedded resources* em `Pedidos.Infrastructure/Migracoes/Scripts`, nomeados com prefixo numérico (`0001_...`), executados em ordem e registrados na tabela `schemaversions`. Cada fatia DEVE (SHALL) trazer seus próprios scripts, e scripts já aplicados NÃO DEVEM (SHALL NOT) ser alterados.

#### Scenario: Banco vazio
- **WHEN** a aplicação inicia com `APLICAR_MIGRACOES=true` contra um banco vazio
- **THEN** todos os scripts existentes são executados em ordem e a tabela `schemaversions` registra cada um

#### Scenario: Idempotência
- **WHEN** a aplicação reinicia sem novos scripts
- **THEN** nenhum script é reexecutado

#### Scenario: Script com erro
- **WHEN** um script falha
- **THEN** a transação do script sofre rollback, o erro é registrado e a aplicação encerra com código diferente de zero

#### Scenario: Migrações desabilitadas
- **WHEN** `APLICAR_MIGRACOES=false`
- **THEN** a aplicação inicia sem alterar o esquema

### Requirement: Espera pelo Banco
O sistema DEVE (SHALL) tentar conectar ao PostgreSQL por até 30 segundos antes de executar as migrações, falhando com mensagem clara se o banco continuar indisponível.

#### Scenario: Banco ainda subindo
- **WHEN** a API inicia antes de o PostgreSQL aceitar conexões
- **THEN** ela repete a tentativa e prossegue quando o banco responde

#### Scenario: Banco indisponível
- **WHEN** o PostgreSQL não responde em 30 segundos
- **THEN** a aplicação encerra com mensagem clara e código de saída diferente de zero
