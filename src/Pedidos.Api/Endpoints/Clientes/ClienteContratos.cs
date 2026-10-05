using FastEndpoints;
using FluentValidation;
using Pedidos.Domain.Entidades;

namespace Pedidos.Api.Endpoints.Clientes;

/// <summary>Representação de um cliente nas respostas da API.</summary>
public sealed record ClienteResponse(Guid Id, string Nome, string Email, DateTimeOffset CriadoEm);

/// <summary>Campos editáveis do cliente, comuns à criação e à atualização.</summary>
public abstract class DadosDoClienteRequest
{
    public string Nome { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}

/// <summary>Request dos endpoints que recebem apenas o id do cliente pela rota.</summary>
public sealed class ClientePorIdRequest
{
    public Guid Id { get; init; }
}

public static class ClienteValidacao
{
    /// <summary>Mesmas regras da entidade <see cref="Cliente"/>, respondidas como 400 por campo.</summary>
    public static void AplicarRegrasDoCliente<T>(this AbstractValidator<T> validator)
        where T : DadosDoClienteRequest
    {
        ArgumentNullException.ThrowIfNull(validator);

        validator.RuleFor(r => r.Nome)
            .Must(Cliente.NomeEhValido)
            .WithMessage($"nome deve ter entre 1 e {Cliente.NomeTamanhoMaximo} caracteres.");

        validator.RuleFor(r => r.Email)
            .Must(Cliente.EmailEhValido)
            .WithMessage($"email deve ser um e-mail válido com até {Cliente.EmailTamanhoMaximo} caracteres.");
    }
}
