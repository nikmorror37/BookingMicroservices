# multi-stage build for BookingWebApp
# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# копируем csproj и nuget.restore
COPY src/Web/BookingWebApp/BookingWebApp.csproj ./Web/BookingWebApp/
COPY src/Contracts/BookingMicro.Contracts.csproj ./Contracts/
# сервисные csproj нужны для корректного Restore зависимости проекта фронта (ProjectReference)
COPY src/Services/BookingService.API/BookingService.API.csproj ./Services/BookingService.API/
COPY src/Services/CatalogService.API/CatalogService.API.csproj ./Services/CatalogService.API/
COPY src/Services/RoomService.API/RoomService.API.csproj ./Services/RoomService.API/
COPY src/Services/PaymentService.API/PaymentService.API.csproj ./Services/PaymentService.API/
COPY src/Services/IdentityService.API/IdentityService.API.csproj ./Services/IdentityService.API/

RUN dotnet restore ./Web/BookingWebApp/BookingWebApp.csproj

# ---------- publish stage ----------
COPY . .
RUN dotnet publish ./src/Web/BookingWebApp/BookingWebApp.csproj -c Release -o /app/publish

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "BookingWebApp.dll"] 