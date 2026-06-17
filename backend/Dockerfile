# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first (leverages layer caching when only source changes)
COPY PixelArt.Api/PixelArt.Api.csproj PixelArt.Api/
RUN dotnet restore PixelArt.Api/PixelArt.Api.csproj

# Copy the rest and publish
COPY . .
RUN dotnet publish PixelArt.Api/PixelArt.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Kestrel listens on 8080 inside the container
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "PixelArt.Api.dll"]
