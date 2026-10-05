CREATE TABLE produtos (
    id         integer       GENERATED ALWAYS AS IDENTITY,
    sku        varchar(50)   NOT NULL,
    nome       varchar(150)  NOT NULL,
    preco      numeric(12,2) NOT NULL,
    estoque    integer       NOT NULL,
    criado_em  timestamptz   NOT NULL,
    CONSTRAINT produtos_pkey PRIMARY KEY (id),
    CONSTRAINT ck_produtos_preco CHECK (preco >= 0),
    CONSTRAINT ck_produtos_estoque CHECK (estoque BETWEEN 0 AND 1000000)
);

-- SKU já chega normalizado (sem espaços nas pontas, em maiúsculas): índice único simples.
CREATE UNIQUE INDEX ux_produtos_sku ON produtos (sku);
