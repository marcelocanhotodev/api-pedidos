using System.Text.RegularExpressions;
using Pedidos.Domain.Excecoes;

namespace Pedidos.Domain.Entidades;

/// <summary>
/// Produto do catálogo. Protege as próprias regras: SKU normalizado (sem espaços nas pontas, em maiúsculas),
/// limites de preço e estoque compatíveis com o banco e preço sempre com 2 casas.
/// O estoque só muda por ajuste atômico no repositório; a entidade valida o delta.
/// </summary>
public sealed partial class Produto
{
    public const int SkuTamanhoMaximo = 50;
    public const int NomeTamanhoMaximo = 150;
    public const decimal PrecoMaximo = 9_999_999_999.99m;
    public const int EstoqueMaximo = 1_000_000;
    public const int DeltaMaximo = 1_000_000;

    private Produto(int id, string sku, string nome, decimal preco, int estoque, DateTimeOffset criadoEm)
    {
        Id = id;
        Sku = sku;
        Nome = nome;
        Preco = preco;
        Estoque = estoque;
        CriadoEm = criadoEm;
    }

    /// <summary>Gerado pelo banco; 0 enquanto o produto não foi persistido.</summary>
    public int Id { get; }

    /// <summary>Normalizado e imutável depois da criação.</summary>
    public string Sku { get; }

    public string Nome { get; private set; }

    /// <summary>Sempre com exatamente 2 casas decimais.</summary>
    public decimal Preco { get; private set; }

    public int Estoque { get; }

    public DateTimeOffset CriadoEm { get; }

    /// <exception cref="RegraDeNegocioException">Algum campo viola as regras do produto.</exception>
    public static Produto Criar(string sku, string nome, decimal preco, int estoque, DateTimeOffset agora)
    {
        if (!SkuEhValido(sku))
        {
            throw new RegraDeNegocioException(
                $"O SKU deve ter de 1 a {SkuTamanhoMaximo} caracteres, apenas letras, números, '-', '_' e '.'.");
        }

        if (!EstoqueEhValido(estoque))
        {
            throw new RegraDeNegocioException($"O estoque inicial deve estar entre 0 e {EstoqueMaximo}.");
        }

        var (nomeNormalizado, precoNormalizado) = ValidarNomeEPreco(nome, preco);
        return new Produto(0, NormalizarSku(sku), nomeNormalizado, precoNormalizado, estoque, agora);
    }

    /// <summary>Reconstrói um produto já persistido, sem revalidar.</summary>
    public static Produto Restaurar(int id, string sku, string nome, decimal preco, int estoque, DateTimeOffset criadoEm) =>
        new(id, sku, nome, NormalizarPreco(preco), estoque, criadoEm);

    /// <summary>Substitui nome e preço; SKU e estoque não mudam aqui.</summary>
    /// <exception cref="RegraDeNegocioException">Nome ou preço inválido.</exception>
    public void Atualizar(string nome, decimal preco)
    {
        (Nome, Preco) = ValidarNomeEPreco(nome, preco);
    }

    /// <exception cref="RegraDeNegocioException">Delta zero ou fora do limite.</exception>
    public static void ValidarDelta(int delta)
    {
        if (!DeltaEhValido(delta))
        {
            throw new RegraDeNegocioException($"O ajuste de estoque deve ser diferente de zero e estar entre -{DeltaMaximo} e {DeltaMaximo}.");
        }
    }

    public static string NormalizarSku(string? sku) => (sku ?? string.Empty).Trim().ToUpperInvariant();

    public static bool SkuEhValido(string? sku) => FormatoDeSku().IsMatch(NormalizarSku(sku));

    public static bool NomeEhValido(string? nome) => nome?.Trim().Length is >= 1 and <= NomeTamanhoMaximo;

    /// <summary>Entre 0 e o máximo do <c>numeric(12,2)</c>, com no máximo 2 casas decimais.</summary>
    public static bool PrecoEhValido(decimal preco) =>
        preco is >= 0 and <= PrecoMaximo && decimal.Round(preco, 2) == preco;

    public static bool EstoqueEhValido(int estoque) => estoque is >= 0 and <= EstoqueMaximo;

    public static bool DeltaEhValido(int delta) => delta != 0 && Math.Abs((long)delta) <= DeltaMaximo;

    private static (string Nome, decimal Preco) ValidarNomeEPreco(string nome, decimal preco)
    {
        if (!NomeEhValido(nome))
        {
            throw new RegraDeNegocioException($"O nome do produto deve ter entre 1 e {NomeTamanhoMaximo} caracteres.");
        }

        if (!PrecoEhValido(preco))
        {
            throw new RegraDeNegocioException($"O preço deve estar entre 0 e {PrecoMaximo} e ter no máximo 2 casas decimais.");
        }

        return (nome.Trim(), NormalizarPreco(preco));
    }

    // Soma de 0.00m força a escala de 2 casas: 4.9m vira 4.90m, como o numeric(12,2) devolve.
    private static decimal NormalizarPreco(decimal preco) => decimal.Round(preco, 2) + 0.00m;

    [GeneratedRegex(@"^[A-Z0-9._-]{1,50}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex FormatoDeSku();
}
