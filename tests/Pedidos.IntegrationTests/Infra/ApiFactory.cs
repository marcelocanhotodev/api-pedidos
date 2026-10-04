using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pedidos.IntegrationTests.Infra;

/// <summary>Sobe a API real em memória apontando para o banco informado, com configuração de segurança de teste.</summary>
public sealed class ApiFactory(string connectionString, bool aplicarMigracoes = true, string? jwtChave = ApiFactory.ChaveJwt)
    : WebApplicationFactory<Program>
{
    public const string ChaveJwt = "chave-jwt-de-teste-com-mais-de-32-caracteres";
    public const string Emissor = "pedidos-api-testes";
    public const string Audiencia = "pedidos-clientes-testes";
    public const int ExpiraMinutos = 15;
    public const string Usuario = "admin";
    public const string Senha = "senha-de-teste";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testes");
        builder.UseSetting("ConnectionStrings:Padrao", connectionString);
        builder.UseSetting("APLICAR_MIGRACOES", aplicarMigracoes ? "true" : "false");
        builder.UseSetting("JWT_CHAVE", jwtChave ?? string.Empty);
        builder.UseSetting("JWT_EMISSOR", Emissor);
        builder.UseSetting("JWT_AUDIENCIA", Audiencia);
        builder.UseSetting("JWT_EXPIRA_MINUTOS", ExpiraMinutos.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("AUTH_USUARIO", Usuario);
        builder.UseSetting("AUTH_SENHA", Senha);
    }
}
