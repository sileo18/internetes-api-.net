# --- Estágio de Build ---
# Publica a aplicação
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY ["WordsAPI/WordsAPI.csproj", "WordsAPI/"]
RUN dotnet restore "WordsAPI/WordsAPI.csproj"
COPY ["WordsAPI/", "WordsAPI/"]
WORKDIR "/source/WordsAPI"
RUN dotnet publish "WordsAPI.csproj" -c Release -o /app/publish

# --- Estágio Final ---
# Usa a imagem de runtime do ASP.NET. É o suficiente.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080

# O Ponto de Entrada mais simples possível.
# Apenas executa a sua API. A própria API fará a migration.
ENTRYPOINT ["dotnet", "WordsAPI.dll"]