-- Script propositalmente inválido: a primeira instrução funciona, a segunda falha.
-- Com transação por script, a tabela criada não pode permanecer no banco.
CREATE TABLE tabela_parcial (id int);
SELECT * FROM tabela_que_nao_existe;
