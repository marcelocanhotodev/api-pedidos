# syntax=docker/dockerfile:1

# ---- build: restaura, compila e publica ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro só com os .csproj para aproveitar o cache de camadas
COPY Directory.Build.props .editorconfig ./
COPY src/Pedidos.Domain/Pedidos.Domain.csproj src/Pedidos.Domain/
COPY src/Pedidos.Application/Pedidos.Application.csproj src/Pedidos.Application/
COPY src/Pedidos.Infrastructure/Pedidos.Infrastructure.csproj src/Pedidos.Infrastructure/
COPY src/Pedidos.Api/Pedidos.Api.csproj src/Pedidos.Api/
RUN dotnet restore src/Pedidos.Api/Pedidos.Api.csproj

COPY src/ src/
RUN dotnet publish src/Pedidos.Api/Pedidos.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- runtime: apenas o ASP.NET Core e os binários publicados ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
# Usuário não-root "app" já existente na imagem oficial
USER $APP_UID
ENTRYPOINT ["dotnet", "Pedidos.Api.dll"]
