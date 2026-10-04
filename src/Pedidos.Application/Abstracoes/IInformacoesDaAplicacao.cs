namespace Pedidos.Application.Abstracoes;

/// <summary>Dados de identificação da aplicação em execução.</summary>
public interface IInformacoesDaAplicacao
{
    string Nome { get; }

    string Versao { get; }

    string Ambiente { get; }
}
