# -------- BUILD STAGE --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layer
COPY src/Services/ApiGateway/ApiGateway.csproj ./src/Services/ApiGateway/
RUN dotnet restore ./src/Services/ApiGateway/ApiGateway.csproj

# copy everything and publish
COPY . .
WORKDIR /src/src/Services/ApiGateway
RUN dotnet publish ApiGateway.csproj -c Release -o /app/publish /p:UseAppHost=false

# -------- RUNTIME STAGE --------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "ApiGateway.dll"] 