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

# 1. Copia os arquivos publicados (as DLLs, etc.)
COPY --from=build /app/publish .

# ======================= A CORREÇÃO FINAL ESTÁ AQUI =======================
# 2. Copia o arquivo .csproj para o diretório de trabalho.
#    Isso dá ao 'dotnet ef' o metadado que ele precisa.
COPY --from=build /source/WordsAPI/WordsAPI.csproj .
# ==========================================================================

EXPOSE 8080

# Instala a ferramenta 'ef'
RUN dotnet tool install --global dotnet-ef

# Adiciona o diretório de ferramentas ao PATH
ENV PATH="$PATH:/root/.dotnet/tools"

# Este ENTRYPOINT agora vai funcionar porque o .csproj está no mesmo diretório.
ENTRYPOINT ["/bin/sh", "-c", "dotnet ef database update --connection \"$DATABASE_URL\" && dotnet WordsAPI.dll"]