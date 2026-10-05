-- Troca a chave de clientes de uuid para integer autoincremental, preservando os dados.
-- A tabela é recriada (ainda não há chaves estrangeiras) para que colunas, nomes e índices fiquem
-- idênticos aos de um banco novo, e os ids sigam a ordem de cadastro.

CREATE TABLE clientes_novo (
    id         integer      GENERATED ALWAYS AS IDENTITY,
    nome       varchar(150) NOT NULL,
    email      varchar(200) NOT NULL,
    criado_em  timestamptz  NOT NULL
);

INSERT INTO clientes_novo (nome, email, criado_em)
SELECT nome, email, criado_em
FROM clientes
ORDER BY criado_em, id;

DROP TABLE clientes;

ALTER TABLE clientes_novo RENAME TO clientes;
ALTER TABLE clientes ADD CONSTRAINT clientes_pkey PRIMARY KEY (id);
ALTER SEQUENCE clientes_novo_id_seq RENAME TO clientes_id_seq;

-- E-mail único sem diferenciar maiúsculas e minúsculas.
CREATE UNIQUE INDEX ux_clientes_email ON clientes (lower(email));
