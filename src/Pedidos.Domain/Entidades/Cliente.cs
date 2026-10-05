using System.Text.RegularExpressions;
using Pedidos.Domain.Excecoes;

namespace Pedidos.Domain.Entidades;

/// <summary>
/// Cliente que faz pedidos. Protege as próprias regras: nome e e-mail são normalizados (sem espaços nas pontas)
/// e validados em qualquer caminho de criação ou alteração, não só na entrada HTTP.
/// </summary>
public sealed partial class Cliente
{
    public const int NomeTamanhoMaximo = 150;
    public const int EmailTamanhoMaximo = 200;

    private Cliente(int id, string nome, string email, DateTimeOffset criadoEm)
    {
        Id = id;
        Nome = nome;
        Email = email;
        CriadoEm = criadoEm;
    }

    /// <summary>Gerado pelo banco; 0 enquanto o cliente não foi persistido.</summary>
    public int Id { get; }

    public string Nome { get; private set; }

    public string Email { get; private set; }

    public DateTimeOffset CriadoEm { get; }

    /// <summary>Cria um novo cliente, ainda sem id (o banco gera o id ao inserir).</summary>
    /// <exception cref="RegraDeNegocioException">Nome ou e-mail inválido.</exception>
    public static Cliente Criar(string nome, string email, DateTimeOffset agora)
    {
        var (nomeNormalizado, emailNormalizado) = Validar(nome, email);
        return new Cliente(0, nomeNormalizado, emailNormalizado, agora);
    }

    /// <summary>Reconstrói um cliente já persistido, sem revalidar.</summary>
    public static Cliente Restaurar(int id, string nome, string email, DateTimeOffset criadoEm) =>
        new(id, nome, email, criadoEm);

    /// <summary>Substitui nome e e-mail.</summary>
    /// <exception cref="RegraDeNegocioException">Nome ou e-mail inválido.</exception>
    public void Atualizar(string nome, string email)
    {
        (Nome, Email) = Validar(nome, email);
    }

    public static bool NomeEhValido(string? nome) =>
        nome?.Trim().Length is >= 1 and <= NomeTamanhoMaximo;

    public static bool EmailEhValido(string? email)
    {
        var normalizado = email?.Trim();
        return normalizado is { Length: > 0 and <= EmailTamanhoMaximo } && FormatoDeEmail().IsMatch(normalizado);
    }

    private static (string Nome, string Email) Validar(string nome, string email)
    {
        if (!NomeEhValido(nome))
        {
            throw new RegraDeNegocioException($"O nome do cliente deve ter entre 1 e {NomeTamanhoMaximo} caracteres.");
        }

        if (!EmailEhValido(email))
        {
            throw new RegraDeNegocioException($"O e-mail do cliente é inválido ou tem mais de {EmailTamanhoMaximo} caracteres.");
        }

        return (nome.Trim(), email.Trim());
    }

    // Formato básico: algo@dominio.tld, sem espaços.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex FormatoDeEmail();
}
