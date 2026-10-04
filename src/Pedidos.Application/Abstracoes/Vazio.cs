namespace Pedidos.Application.Abstracoes;

/// <summary>Saída de casos de uso que não retornam valor.</summary>
public sealed record Vazio
{
    public static readonly Vazio Valor = new();

    private Vazio()
    {
    }
}
