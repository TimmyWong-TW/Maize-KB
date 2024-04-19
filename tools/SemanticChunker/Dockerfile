FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/nightly/sdk:8.0-alpine-aot AS build
RUN apk add --no-cache clang zlib-dev
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SemanticChunker/SemanticChunker.csproj", "SemanticChunker/"]
RUN dotnet restore "./SemanticChunker/SemanticChunker.csproj"
COPY . .
WORKDIR "/src/SemanticChunker"
RUN dotnet build "./SemanticChunker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SemanticChunker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

FROM mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-alpine-aot AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./SemanticChunker"]