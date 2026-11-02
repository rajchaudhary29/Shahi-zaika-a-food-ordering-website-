# ==========================================
# Multi-stage Dockerfile for Shahi Zayka API
# ==========================================

# 1. Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution and project files first for optimal Docker layer caching
COPY ShahiZayka.slnx ./
COPY src/ShahiZayka.Domain/*.csproj ./src/ShahiZayka.Domain/
COPY src/ShahiZayka.Application/*.csproj ./src/ShahiZayka.Application/
COPY src/ShahiZayka.Infrastructure/*.csproj ./src/ShahiZayka.Infrastructure/
COPY src/ShahiZayka.Api/*.csproj ./src/ShahiZayka.Api/
COPY tests/ShahiZayka.UnitTests/*.csproj ./tests/ShahiZayka.UnitTests/

RUN dotnet restore ./src/ShahiZayka.Api/ShahiZayka.Api.csproj

# Copy the rest of the source code and build
COPY src/ ./src/
RUN dotnet publish ./src/ShahiZayka.Api/ShahiZayka.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ShahiZayka.Api.dll"]
