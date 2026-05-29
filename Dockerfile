# =========================================
# Base Runtime
# =========================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# =========================================
# Build Stage
# =========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["GymTracker.csproj", "."]
RUN dotnet restore "./GymTracker.csproj"

COPY . .

RUN dotnet build "./GymTracker.csproj" -c $BUILD_CONFIGURATION -o /app/build

# =========================================
# Publish Stage
# =========================================
FROM build AS publish

ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "./GymTracker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# =========================================
# Final Production Image
# =========================================
FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "GymTracker.dll"]