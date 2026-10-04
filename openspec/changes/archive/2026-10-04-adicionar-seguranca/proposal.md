# Change: Adicionar Segurança

## Why
Todos os endpoints de negócio precisam nascer protegidos. Entregar a autenticação JWT logo após a fundação evita
que as fatias de clientes, produtos e pedidos tenham de ser revisitadas para receber proteção depois.

## What Changes
- `POST /api/v1/auth/token`: emite JWT para o usuário configurado por variáveis de ambiente (`GerarTokenUseCase`).
- Proteção por JWT Bearer como padrão de todo endpoint `/api/v1`, exceto a emissão de token.
- Prefixo de versão `/api/v1`, exercitado pela primeira vez por um endpoint desta fatia.
- Swagger passa a declarar o esquema JWT Bearer e o botão Authorize.
- Novas variáveis `JWT_CHAVE`, `JWT_EMISSOR`, `JWT_AUDIENCIA`, `JWT_EXPIRA_MINUTOS`, `AUTH_USUARIO`, `AUTH_SENHA` no `.env.example`.

Depende de: `adicionar-fundacao` (arquivada antes desta).

## Capabilities

### New Capabilities
- `seguranca`: emissão de token, proteção dos endpoints e tratamento de segredos

### Modified Capabilities
- `convencoes-api`: adiciona "Versionamento da API" e altera "Documentação da API" para incluir o esquema JWT Bearer

## Impact
- Código: `Application/CasosDeUso/Auth/GerarToken/`, `Infrastructure/Seguranca/`, `Api/Endpoints/Auth/`, configuração JWT em `Program.cs`.
- Dependência nova: `FastEndpoints.Security`.
- Configuração: seis novas variáveis de ambiente; a aplicação passa a recusar iniciar sem `JWT_CHAVE` válida.
