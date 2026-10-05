CREATE TABLE clientes (
    id         uuid         PRIMARY KEY,
    nome       varchar(150) NOT NULL,
    email      varchar(200) NOT NULL,
    criado_em  timestamptz  NOT NULL
);

-- E-mail único sem diferenciar maiúsculas e minúsculas.
CREATE UNIQUE INDEX ux_clientes_email ON clientes (lower(email));
