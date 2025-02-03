FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY . ./
RUN dotnet restore GeoMarker.Frontiers.Web
RUN dotnet publish GeoMarker.Frontiers.Web -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "GeoMarker.Frontiers.Web.dll"]