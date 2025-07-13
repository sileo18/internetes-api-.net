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

# 1. Copia os arquivos publicados (as DLLs para executar a API) para /app
COPY --from=build /app/publish .

# ======================= A CORREÇÃO FINAL ESTÁ AQUI =======================
# 2. Copia o código-fonte para um subdiretório chamado /src para evitar conflitos.
COPY --from=build /source /src
# ==========================================================================

EXPOSE 8080

# Instala a ferramenta 'ef'
RUN dotnet tool install --global dotnet-ef

# Adiciona o diretório de ferramentas ao PATH
ENV PATH="$PATH:/root/.dotnet/tools"

# Este ENTRYPOINT agora aponta para o projeto no subdiretório /src
# e executa a API a partir do diretório /app
ENTRYPOINT ["/bin/sh", "-c", "dotnet ef database update --project /src/WordsAPI/WordsAPI.csproj --connection \"$DATABASE_URL\" && dotnet WordsAPI.dll"]