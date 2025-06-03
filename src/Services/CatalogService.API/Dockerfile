FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["src/Services/CatalogService.API/CatalogService.API.csproj", "src/Services/CatalogService.API/"]
COPY ["src/Contracts/BookingMicro.Contracts.csproj", "src/Contracts/"]

RUN dotnet restore "src/Services/CatalogService.API/CatalogService.API.csproj"

# Copy everything else and build
COPY ["src/Services/CatalogService.API/", "src/Services/CatalogService.API/"]
COPY ["src/Contracts/", "src/Contracts/"]

WORKDIR "/src/src/Services/CatalogService.API"
RUN dotnet build "CatalogService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CatalogService.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "CatalogService.API.dll"] 