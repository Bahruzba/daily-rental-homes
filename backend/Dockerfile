# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/DailyRentalHomes.slnx backend/global.json backend/dotnet-tools.json ./
COPY backend/src ./src
COPY backend/tests ./tests

RUN dotnet restore DailyRentalHomes.slnx
RUN dotnet publish src/DailyRentalHomes.Api/DailyRentalHomes.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DailyRentalHomes.Api.dll"]
