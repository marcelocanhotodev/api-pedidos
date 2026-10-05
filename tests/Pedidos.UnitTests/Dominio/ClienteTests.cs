using Pedidos.Domain.Entidades;
using Pedidos.Domain.Excecoes;

namespace Pedidos.UnitTests.Dominio;

/// <summary>Requisitos: Criar Cliente (normalização e validação), Atualizar Cliente.</summary>
public class ClienteTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Criar_DadosValidos_PreencheIdV7ECriadoEm()
    {
        var cliente = Cliente.Criar("Ana", "ana@x.com", Agora);

        Assert.NotEqual(Guid.Empty, cliente.Id);
        Assert.Equal(7, cliente.Id.Version);
        Assert.Equal(Agora, cliente.CriadoEm);
    }

    [Fact]
    public void Criar_EspacosNasPontas_Normaliza()
    {
        var cliente = Cliente.Criar("  Ana  ", " ana@x.com ", Agora);

        Assert.Equal("Ana", cliente.Nome);
        Assert.Equal("ana@x.com", cliente.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_NomeVazio_LancaRegraDeNegocio(string nome)
    {
        Assert.Throws<RegraDeNegocioException>(() => Cliente.Criar(nome, "ana@x.com", Agora));
    }

    [Fact]
    public void Criar_NomeNoLimite_Aceita()
    {
        var nome = new string('a', Cliente.NomeTamanhoMaximo);

        Assert.Equal(nome, Cliente.Criar(nome, "ana@x.com", Agora).Nome);
    }

    [Fact]
    public void Criar_NomeAcimaDoLimite_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(() => Cliente.Criar(new string('a', Cliente.NomeTamanhoMaximo + 1), "ana@x.com", Agora));
    }

    [Theory]
    [InlineData("invalido")]
    [InlineData("ana@x")]
    [InlineData("ana @x.com")]
    [InlineData("")]
    public void Criar_EmailInvalido_LancaRegraDeNegocio(string email)
    {
        Assert.Throws<RegraDeNegocioException>(() => Cliente.Criar("Ana", email, Agora));
    }

    [Fact]
    public void Criar_EmailAcimaDoLimite_LancaRegraDeNegocio()
    {
        var email = new string('a', Cliente.EmailTamanhoMaximo - "@x.com".Length + 1) + "@x.com";

        Assert.Throws<RegraDeNegocioException>(() => Cliente.Criar("Ana", email, Agora));
    }

    [Fact]
    public void Atualizar_DadosValidos_SubstituiENormaliza()
    {
        var cliente = Cliente.Criar("Ana", "ana@x.com", Agora);

        cliente.Atualizar(" Ana Souza ", " ana.souza@x.com ");

        Assert.Equal("Ana Souza", cliente.Nome);
        Assert.Equal("ana.souza@x.com", cliente.Email);
        Assert.Equal(Agora, cliente.CriadoEm);
    }

    [Fact]
    public void Atualizar_EmailInvalido_LancaEMantemDados()
    {
        var cliente = Cliente.Criar("Ana", "ana@x.com", Agora);

        Assert.Throws<RegraDeNegocioException>(() => cliente.Atualizar("Ana", "invalido"));
        Assert.Equal("ana@x.com", cliente.Email);
    }

    [Fact]
    public void Restaurar_DadosDoBanco_ReconstroiSemAlterar()
    {
        var id = Guid.NewGuid();

        var cliente = Cliente.Restaurar(id, "Ana", "Ana@X.com", Agora);

        Assert.Equal(id, cliente.Id);
        Assert.Equal("Ana@X.com", cliente.Email);
    }
}
