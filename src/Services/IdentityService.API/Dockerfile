# -------- BUILD STAGE --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Services/IdentityService.API/IdentityService.API.csproj ./src/Services/IdentityService.API/
COPY src/Contracts/BookingMicro.Contracts.csproj ./src/Contracts/
RUN dotnet restore ./src/Services/IdentityService.API/IdentityService.API.csproj

COPY . .
WORKDIR /src/src/Services/IdentityService.API
RUN dotnet publish IdentityService.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "IdentityService.API.dll"] 