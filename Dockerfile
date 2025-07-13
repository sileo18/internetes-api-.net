# --- Estágio de Build ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# 1. Copia o arquivo .csproj PRIMEIRO
#    O caminho agora inclui a pasta "WordsAPI"
COPY ["WordsAPI/WordsAPI.csproj", "WordsAPI/"]

# 2. Restaura as dependências usando o caminho correto
#    Isso aproveita o cache de layers do Docker
RUN dotnet restore "WordsAPI/WordsAPI.csproj"

# 3. Copia o restante do código do projeto
COPY ["WordsAPI/", "WordsAPI/"]

# 4. Publica a aplicação, especificando o projeto a ser publicado
WORKDIR "/source/WordsAPI"
RUN dotnet publish "WordsAPI.csproj" -c Release -o /app/publish

# --- Estágio Final ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["/bin/sh", "-c", "dotnet ef database update --connection \"$DATABASE_URL\" -- --environment Production && dotnet WordsAPI.dll"]