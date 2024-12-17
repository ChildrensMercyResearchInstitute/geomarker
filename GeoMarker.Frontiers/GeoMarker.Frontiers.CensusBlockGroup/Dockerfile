FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . ./

RUN dotnet restore GeoMarker.Frontiers.CensusBlockGroup
RUN dotnet publish GeoMarker.Frontiers.CensusBlockGroup -c Release -o /app /p:UseAppHost=false
ADD GeoMarker.Frontiers.CensusBlockGroup/entrypoint_json.R /app

FROM ghcr.io/degauss-org/census_block_group:0.6.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
EXPOSE 443

RUN apt-get update && apt-get install curl --yes
RUN curl https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb --output packages-microsoft-prod.deb \
	&& dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb

RUN apt-get update \
    && apt-get upgrade -y \
    && apt-get install -y aspnetcore-runtime-8.0

ENV DOTNET_VERSION=8.0.0

ENTRYPOINT ["dotnet", "GeoMarker.Frontiers.CensusBlockGroup.dll"]