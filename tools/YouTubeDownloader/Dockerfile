FROM mcr.microsoft.com/dotnet/nightly/runtime:8.0-alpine AS base
RUN apk add ffmpeg
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/nightly/sdk:8.0-alpine-aot AS build
RUN apk add --no-cache clang zlib-dev
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["YouTubeDownloader/YouTubeDownloader.csproj", "YouTubeDownloader/"]
RUN dotnet restore "./YouTubeDownloader/YouTubeDownloader.csproj"
COPY . .
WORKDIR "/src/YouTubeDownloader"
RUN dotnet build "./YouTubeDownloader.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./YouTubeDownloader.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

FROM mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-alpine-aot AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["/app/YouTubeDownloader"]