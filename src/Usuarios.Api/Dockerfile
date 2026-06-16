# --------------------------------------------------------------------------------------------------
# STAGE: BASE (Execução/Runtime)
# Onde a aplicação irá rodar.
# --------------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Cria um grupo e um usuário não-root (FIAP) com um ID específico (ex: 1000)
# Usar um UID/GID fixo ajuda na gestão de permissões de volumes.
ARG APP_USER=FIAP
ARG APP_UID=1000
RUN groupadd -r $APP_USER -g $APP_UID && \
    useradd -r -g $APP_USER -u $APP_UID -s /sbin/nologin -c "Docker User" $APP_USER

WORKDIR /app

# Define o usuário não-root para execução.
USER $APP_USER 

#http
EXPOSE 8080



# --------------------------------------------------------------------------------------------------
# STAGE: BUILD
# Usa o SDK para compilar. 
# --------------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Usuarios.Api/Usuarios.Api.csproj", "src/Usuarios.Api/"]
RUN dotnet restore "./src/Usuarios.Api/Usuarios.Api.csproj"
COPY . .
WORKDIR "/src/src/Usuarios.Api"
RUN dotnet build "./Usuarios.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build



# --------------------------------------------------------------------------------------------------
# STAGE: PUBLISH
# --------------------------------------------------------------------------------------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Usuarios.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false



# --------------------------------------------------------------------------------------------------
# STAGE: FINAL (Produção)
# Copia os binários e garante que o usuário FIAP tem as permissões corretas
# --------------------------------------------------------------------------------------------------
FROM base AS final
WORKDIR /app
USER $APP_USER
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Usuarios.Api.dll"]