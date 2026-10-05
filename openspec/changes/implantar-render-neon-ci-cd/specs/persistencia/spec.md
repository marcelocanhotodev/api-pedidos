## ADDED Requirements

### Requirement: Migração como Etapa de Implantação
A aplicação DEVE (SHALL) oferecer o modo `--migrar`, que aguarda o banco, aplica as migrações pendentes e encerra sem subir o servidor HTTP, com código de saída `0` em sucesso e diferente de zero em falha, sem exigir a configuração de segurança (JWT e credenciais), que só é necessária para servir a API.

#### Scenario: Migração bem-sucedida
- **WHEN** a aplicação é executada com `--migrar` contra um banco com migrações pendentes
- **THEN** as migrações são aplicadas, nenhuma porta HTTP é aberta e o processo encerra com código `0`

#### Scenario: Nada a migrar
- **WHEN** a aplicação é executada com `--migrar` contra um banco já atualizado
- **THEN** nenhum script é executado e o processo encerra com código `0`

#### Scenario: Migração com erro
- **WHEN** um script falha durante `--migrar`
- **THEN** a transação do script é desfeita, o erro é registrado e o processo encerra com código diferente de zero

#### Scenario: Sem configuração de segurança
- **WHEN** a aplicação é executada com `--migrar` sem `JWT_CHAVE` nem `AUTH_*` definidos
- **THEN** as migrações são aplicadas normalmente
