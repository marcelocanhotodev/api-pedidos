using Dapper;
using Npgsql;
using Pedidos.Domain.Entidades;
using Pedidos.Domain.Excecoes;
using Pedidos.Domain.Repositorios;

namespace Pedidos.Infrastructure.Dados.Repositorios;

internal sealed class ProdutoRepository(IDbSession sessao) : IProdutoRepository
{
    private const string IndiceSkuUnico = "ux_produtos_sku";
    private const string Colunas = "id, sku, nome, preco, estoque, criado_em";

    public async Task<Produto?> ObterAsync(int id, CancellationToken ct)
    {
        var conexao = await sessao.ObterConexaoAsync(ct);
        var linha = await conexao.QuerySingleOrDefaultAsync<ProdutoLinha>(new CommandDefinition(
            $"SELECT {Colunas} FROM produtos WHERE id = @id", new { id }, sessao.Transacao, cancellationToken: ct));
        return linha?.ParaEntidade();
    }

    public async Task<bool> ExisteSkuAsync(string sku, CancellationToken ct)
    {
        var conexao = await sessao.ObterConexaoAsync(ct);
        return await conexao.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM produtos WHERE sku = @sku)", new { sku }, sessao.Transacao, cancellationToken: ct));
    }

    public async Task<Produto> InserirAsync(Produto produto, CancellationToken ct)
    {
        var conexao = await sessao.ObterConexaoAsync(ct);
        try
        {
            // O id é gerado pelo banco (identity) e devolvido no próprio INSERT.
            var id = await conexao.ExecuteScalarAsync<int>(new CommandDefinition(
                """
                INSERT INTO produtos (sku, nome, preco, estoque, criado_em)
                VALUES (@Sku, @Nome, @Preco, @Estoque, @CriadoEm)
                RETURNING id
                """,
                new { produto.Sku, produto.Nome, produto.Preco, produto.Estoque, produto.CriadoEm },
                sessao.Transacao,
                cancellationToken: ct));
            return Produto.Restaurar(id, produto.Sku, produto.Nome, produto.Preco, produto.Estoque, produto.CriadoEm);
        }
        catch (PostgresException ex) when (ex is { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: IndiceSkuUnico })
        {
            throw new ConflitoException($"Já existe um produto com o SKU '{produto.Sku}'.");
        }
    }

    public async Task AtualizarNomeEPrecoAsync(Produto produto, CancellationToken ct)
    {
        // Só nome e preço: gravar o estoque lido antes desfaria um ajuste concorrente.
        var conexao = await sessao.ObterConexaoAsync(ct);
        await conexao.ExecuteAsync(new CommandDefinition(
            "UPDATE produtos SET nome = @Nome, preco = @Preco WHERE id = @Id",
            new { produto.Id, produto.Nome, produto.Preco },
            sessao.Transacao,
            cancellationToken: ct));
    }

    public async Task<Produto> AjustarEstoqueAsync(int id, int delta, CancellationToken ct)
    {
        var conexao = await sessao.ObterConexaoAsync(ct);

        // Uma única instrução: a linha fica bloqueada até o fim da transação e, sob concorrência, o PostgreSQL
        // reavalia o WHERE com o valor já confirmado — nenhum ajuste se perde. A soma em bigint nunca estoura.
        var linha = await conexao.QuerySingleOrDefaultAsync<ProdutoLinha>(new CommandDefinition(
            $"""
            UPDATE produtos SET estoque = estoque + @delta
            WHERE id = @id AND estoque::bigint + @delta BETWEEN 0 AND @maximo
            RETURNING {Colunas}
            """,
            new { id, delta, maximo = Produto.EstoqueMaximo },
            sessao.Transacao,
            cancellationToken: ct));

        if (linha is not null)
        {
            return linha.ParaEntidade();
        }

        var existe = await conexao.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM produtos WHERE id = @id)", new { id }, sessao.Transacao, cancellationToken: ct));

        throw existe
            ? new RegraDeNegocioException($"O ajuste deixaria o estoque fora do intervalo de 0 a {Produto.EstoqueMaximo}.")
            : new NaoEncontradoException("Produto", id);
    }

    public async Task<PaginaDeProdutos> ListarAsync(string? busca, int deslocamento, int quantidade, CancellationToken ct)
    {
        // count(*) OVER() devolve o total junto com a página, numa única ida ao banco.
        const string Filtro = """
            WHERE @padrao::text IS NULL
               OR nome ILIKE @padrao ESCAPE '\'
               OR sku ILIKE @padrao ESCAPE '\'
            """;
        var parametros = new { padrao = BuscaTextual.Padrao(busca), deslocamento, quantidade };

        var conexao = await sessao.ObterConexaoAsync(ct);
        var linhas = (await conexao.QueryAsync<ProdutoLinha>(new CommandDefinition(
            $"""
            SELECT {Colunas}, count(*) OVER() AS total
            FROM produtos
            {Filtro}
            ORDER BY nome, id
            OFFSET @deslocamento LIMIT @quantidade
            """,
            parametros,
            sessao.Transacao,
            cancellationToken: ct))).ToList();

        // Página além do fim: não há linhas para carregar o total, então ele é contado à parte.
        var total = linhas.Count > 0
            ? linhas[0].Total
            : await conexao.ExecuteScalarAsync<long>(new CommandDefinition(
                $"SELECT count(*) FROM produtos {Filtro}", parametros, sessao.Transacao, cancellationToken: ct));

        return new PaginaDeProdutos(linhas.Select(l => l.ParaEntidade()).ToList(), total);
    }

    private sealed class ProdutoLinha
    {
        public int Id { get; init; }

        public string Sku { get; init; } = string.Empty;

        public string Nome { get; init; } = string.Empty;

        public decimal Preco { get; init; }

        public int Estoque { get; init; }

        public DateTimeOffset CriadoEm { get; init; }

        public long Total { get; init; }

        public Produto ParaEntidade() => Produto.Restaurar(Id, Sku, Nome, Preco, Estoque, CriadoEm);
    }
}
