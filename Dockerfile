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

# Instala a ferramenta 'ef'
RUN dotnet tool install --global dotnet-ef

# Adiciona o diretório de ferramentas ao PATH
ENV PATH="$PATH:/root/.dotnet/tools"

# ======================= A CORREÇÃO FINAL ESTÁ AQUI =======================
# Adicionamos a flag '--startup-project' para dizer ao 'ef' qual DLL inspecionar.
ENTRYPOINT ["/bin/sh", "-c", "dotnet ef database update --startup-project WordsAPI.dll --connection \"$DATABASE_URL\" && dotnet WordsAPI.dll"]
# ==========================================================================