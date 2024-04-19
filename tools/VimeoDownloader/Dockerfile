FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS base
RUN apk add ffmpeg
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/nightly/sdk:8.0-alpine-aot AS build
RUN apk add --no-cache clang zlib-dev
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VimeoDownloader/VimeoDownloader.csproj", "VimeoDownloader/"]
RUN dotnet restore "./VimeoDownloader/VimeoDownloader.csproj"
COPY . .
WORKDIR "/src/VimeoDownloader"
RUN dotnet build "./VimeoDownloader.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VimeoDownloader.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

FROM mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-alpine-aot AS final
RUN apk add ffmpeg
COPY --from=publish /app/publish /app
ENTRYPOINT ["/app/VimeoDownloader"]