# ==========================================
# Multi-stage Dockerfile for Shahi Zayka API
# Backend Context
# ==========================================

# 1. Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY . ./
RUN dotnet restore src/ShahiZayka.Api/ShahiZayka.Api.csproj
RUN dotnet publish src/ShahiZayka.Api/ShahiZayka.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ShahiZayka.Api.dll"]
