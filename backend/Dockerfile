# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/core/domain/PixelArt.Core.Domain.csproj src/core/domain/
COPY src/core/abstraction/PixelArt.Core.Abstraction.csproj src/core/abstraction/
COPY src/core/application/PixelArt.Core.Application.csproj src/core/application/
COPY src/external/infrastructure/PixelArt.External.Infrastructure.csproj src/external/infrastructure/
COPY src/external/interface/PixelArt.External.Interface.csproj src/external/interface/
RUN dotnet restore src/external/interface/PixelArt.External.Interface.csproj

# Copy the rest and publish the host project
COPY . .
RUN dotnet publish src/external/interface/PixelArt.External.Interface.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Kestrel listens on 8080 inside the container
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "PixelArt.External.Interface.dll"]
