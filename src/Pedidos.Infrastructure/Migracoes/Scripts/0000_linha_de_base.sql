-- Linha de base do esquema.
-- Garante que o pipeline de migrações (scripts embutidos + tabela schemaversions) funcione desde a fundação.
-- Cada fatia acrescenta seus próprios scripts: 0001_criar_clientes, 0002_criar_produtos, 0003_criar_pedidos.
SET TIME ZONE 'UTC';
