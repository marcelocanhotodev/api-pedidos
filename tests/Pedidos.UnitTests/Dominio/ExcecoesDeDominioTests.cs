using Pedidos.Domain.Excecoes;

namespace Pedidos.UnitTests.Dominio;

public class ExcecoesDeDominioTests
{
    [Fact]
    public void NaoEncontradoException_ComRecursoEId_MontaMensagemEExpoeDados()
    {
        var id = Guid.NewGuid();

        var excecao = new NaoEncontradoException("Cliente", id);

        Assert.Equal($"Cliente '{id}' não foi encontrado.", excecao.Message);
        Assert.Equal("Cliente", excecao.Recurso);
        Assert.Equal(id, excecao.Id);
        Assert.IsType<DominioException>(excecao, exactMatch: false);
    }

    [Fact]
    public void ConflitoException_ComMensagem_PreservaMensagem()
    {
        var excecao = new ConflitoException("E-mail já cadastrado.");

        Assert.Equal("E-mail já cadastrado.", excecao.Message);
        Assert.IsType<DominioException>(excecao, exactMatch: false);
    }

    [Fact]
    public void RegraDeNegocioException_ComMensagem_PreservaMensagem()
    {
        var excecao = new RegraDeNegocioException("Estoque insuficiente.");

        Assert.Equal("Estoque insuficiente.", excecao.Message);
        Assert.IsType<DominioException>(excecao, exactMatch: false);
    }
}
