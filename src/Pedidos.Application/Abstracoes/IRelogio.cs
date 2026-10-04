namespace Pedidos.Application.Abstracoes;

/// <summary>Fonte da data/hora atual, substituível em testes.</summary>
public interface IRelogio
{
    DateTimeOffset AgoraUtc { get; }
}
