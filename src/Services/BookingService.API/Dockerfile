# -------- BUILD STAGE --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Services/BookingService.API/BookingService.API.csproj ./src/Services/BookingService.API/
# copy contracts csproj for restore
COPY src/Contracts/BookingMicro.Contracts.csproj ./src/Contracts/
RUN dotnet restore ./src/Services/BookingService.API/BookingService.API.csproj

COPY . .
WORKDIR /src/src/Services/BookingService.API
RUN dotnet publish BookingService.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ----- RUNTIME -----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "BookingService.API.dll"] 