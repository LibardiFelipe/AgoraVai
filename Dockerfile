FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AgoraVai.WebAPI/AgoraVai.WebAPI.csproj", "AgoraVai.WebAPI/"]
RUN dotnet restore "./AgoraVai.WebAPI/AgoraVai.WebAPI.csproj"
COPY . .
WORKDIR "/src/AgoraVai.WebAPI"
RUN dotnet build "./AgoraVai.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AgoraVai.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

FROM base AS final

# Instala a PRAGA do curl
USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

USER app

WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["./AgoraVai.WebAPI"]