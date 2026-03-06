FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# Поддержка Render (10000) и локальной разработки (8080)
EXPOSE 8080 8081 10000

#RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["DocSenseV1/DocSenseV1.csproj", "DocSenseV1/"]
RUN dotnet restore "DocSenseV1/DocSenseV1.csproj"

COPY . .
WORKDIR "/src/DocSenseV1"
RUN dotnet build "DocSenseV1.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DocSenseV1.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Переменные окружения (берутся из .env или от системы)
ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Development}
ENV ASPNETCORE_URLS=${ASPNETCORE_URLS:-http://+:8080}

ENTRYPOINT ["dotnet", "DocSenseV1.dll"]
