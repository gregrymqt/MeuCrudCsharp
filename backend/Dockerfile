# -----------------------------------------------------------------------------
# Estágio 1: Build (A Oficina de Montagem)
# -----------------------------------------------------------------------------
# Usamos a imagem SDK (Software Development Kit) para compilar o projeto
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copia o arquivo .csproj (que está na raiz do contexto)
#    Copiamos SÓ o .csproj primeiro para otimizar o cache do Docker.
#    Se o projeto não mudar, o 'restore' não é executado de novo.
COPY ["MeuCrudCsharp.csproj", "."]

# 2. Restaura as dependências
RUN dotnet restore "MeuCrudCsharp.csproj"

# 3. Copia todo o resto do código-fonte
COPY . .

# 4. Constrói o projeto. 
#    Não precisamos mais mudar de pasta (WORKDIR)
RUN dotnet build "MeuCrudCsharp.csproj" -c Release -o /app/build /p:TypeScriptCompileBlocked=true

# -----------------------------------------------------------------------------
# Estágio 2: Publish (O Empacotamento Final)
# -----------------------------------------------------------------------------
FROM build AS publish
# O WORKDIR herdado do estágio 'build' ainda é /src
RUN dotnet publish "MeuCrudCsharp.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:TypeScriptCompileBlocked=true

# -----------------------------------------------------------------------------
# Estágio 3: Final (O Showroom)
# -----------------------------------------------------------------------------
# Usamos a imagem ASP.NET, que é menor e otimizada para produção
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# O ENTRYPOINT permanece o mesmo, pois o nome da DLL é baseado no nome do projeto
ENTRYPOINT ["dotnet", "MeuCrudCsharp.dll"]

