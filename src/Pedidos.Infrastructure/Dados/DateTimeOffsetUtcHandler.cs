using System.Data;
using Dapper;

namespace Pedidos.Infrastructure.Dados;

/// <summary>
/// Lê colunas <c>timestamptz</c> (que o Npgsql devolve como <see cref="DateTime"/> UTC) como <see cref="DateTimeOffset"/>
/// e grava <see cref="DateTimeOffset"/> sempre convertido para UTC, como o Npgsql exige.
/// </summary>
internal sealed class DateTimeOffsetUtcHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override DateTimeOffset Parse(object value) => value switch
    {
        DateTimeOffset dto => dto.ToUniversalTime(),
        DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
        _ => throw new DataException($"Não é possível converter {value.GetType().Name} em DateTimeOffset."),
    };

    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        parameter.Value = value.ToUniversalTime();
    }
}
