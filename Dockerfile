# --- Estágio de Build ---
# Não muda nada aqui
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY ["WordsAPI/WordsAPI.csproj", "WordsAPI/"]
RUN dotnet restore "WordsAPI/WordsAPI.csproj"
COPY ["WordsAPI/", "WordsAPI/"]
WORKDIR "/source/WordsAPI"
RUN dotnet publish "WordsAPI.csproj" -c Release -o /app/publish

# --- Estágio Final ---
# === MUDANÇA CRUCIAL AQUI ===
# Em vez de 'aspnet', usamos 'sdk' para ter acesso às ferramentas 'ef'.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
# === FIM DA MUDANÇA ===

WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080

# Este ENTRYPOINT agora vai funcionar porque a imagem contém o SDK.
ENTRYPOINT ["/bin/sh", "-c", "dotnet ef database update --connection \"$DATABASE_URL\" && dotnet WordsAPI.dll"]