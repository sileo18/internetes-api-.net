# --- Estágio de Build ---
# Construímos a aplicação e publicamos para /app/publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY ["WordsAPI/WordsAPI.csproj", "WordsAPI/"]
RUN dotnet restore "WordsAPI/WordsAPI.csproj"
COPY ["WordsAPI/", "WordsAPI/"]
WORKDIR "/source/WordsAPI"
RUN dotnet publish "WordsAPI.csproj" -c Release -o /app/publish

# --- Estágio Final ---
# Usamos a imagem do SDK para ter todas as ferramentas
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app

# 1. Copia os arquivos publicados (as DLLs para executar a API)
COPY --from=build /app/publish .

# ======================= A CORREÇÃO FINAL ESTÁ AQUI =======================
# 2. Copia TODO o código-fonte para um subdiretório.
#    Isso é necessário para que 'dotnet ef' possa compilar o projeto.
COPY --from=build /source .
# ==========================================================================

EXPOSE 8080

# Instala a ferramenta 'ef'
RUN dotnet tool install --global dotnet-ef

# Adiciona o diretório de ferramentas ao PATH
ENV PATH="$PATH:/root/.dotnet/tools"

# Este ENTRYPOINT agora VAI funcionar porque ele aponta para o projeto
# dentro do código-fonte que acabamos de copiar.
ENTRYPOINT ["/bin/sh", "-c", "dotnet ef database update --project WordsAPI/WordsAPI.csproj --connection \"$DATABASE_URL\" && dotnet WordsAPI.dll"]