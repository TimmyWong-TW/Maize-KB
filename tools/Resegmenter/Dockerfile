FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/nightly/sdk:8.0-alpine-aot AS build
RUN apk add clang zlib-dev
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Resegmenter/Resegmenter.csproj", "Resegmenter/"]
RUN dotnet restore "./Resegmenter/Resegmenter.csproj"
COPY . .
WORKDIR "/src/Resegmenter"
RUN dotnet build "./Resegmenter.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Resegmenter.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

FROM mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-alpine-aot AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./Resegmenter"]