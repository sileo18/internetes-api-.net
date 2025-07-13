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
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080

# ======================= A CORREÇÃO FINAL ESTÁ AQUI =======================
# 1. Instala a ferramenta de linha de comando do Entity Framework.
RUN dotnet tool install --global dotnet-ef

# 2. Adiciona o diretório de ferramentas do .NET ao PATH do sistema.
#    Isso garante que o comando 'dotnet-ef' possa ser encontrado pelo shell.
ENV PATH="$PATH:/root/.dotnet/tools"
# ==========================================================================

# Este ENTRYPOINT agora VAI funcionar porque a ferramenta 'dotnet-ef' foi instalada.
ENTRYPOINT ["/bin/sh", "-c", "dotnet ef database update --connection \"$DATABASE_URL\" && dotnet WordsAPI.dll"]