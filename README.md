# Introduction 
This project hosts the multi-service dockerized implementation of the GeoMarker product. 

# Getting Started

## Docker-Compose
From within Visual Studio, docker-compose can be selected as a run profile. The Configuration must be set to 'Release' in order to build the full Docker images for 
the hosted services in the network. 

Alternatively 'docker-compose' may be used from the command line to build, start, and stop the docker containers in the network. 

docker-compose and docker commands must be run from the solutions root directory "...RSE_GeoMarker_Frontiers\GeoMarker.Frontiers".

> docker-compose build
> docker-compose up
> docker-compose down

## Docker

For a single container build, build from the solution root and provide the docker file with '-f' a tag name with '-t' and the context as '.'

> docker build -f GeoMarker.Frontiers.GeoCode\Dockerfile -t [tag] .

## Debugging
To debug a container from Visual Studio, you must select only a single project configuration to launch. e.g. the launch settings "Docker" for "GeoMarker.Frontiers.Geocode" can be used 
to start the service with the debugger attached. 

# Contributing
To add additional services a new project must be added to the solution. Each project represents a single microservice in the network. 

Update the docker-compose.yml and docker-compose.override.yml files at the root of the solution to include the new service in the network.

## DeGauss Images
Each DeGauss image to be supported must be extended to build and shim the .NET REST Apis on to the image. The Dockerfile present in GeoMarker.Frontiers.GeoCode provides
an example of doing so. The final build stage (denoted by the 'FROM') must be the DeGauss image base reference itself, this ensures that the images resources
are present for consumption by the REST API added. 

The GeoMarker.Frontiers.GeoCode docker build installs the ASP .NET core runtime on the image with apt-get and curl. 

# Client Customization
This web app allows customization of the company name in the footer as well as the company logo.

## Company Variables
- COMPANY_NAME: string of the company's name to put in the footer
- COMPANY_LOGO_URL: url of where the company's logo is stored
- COMPANY_LOGO_HEIGHT: height in pixels of the logo
- COMPANY_LOGO_WIDTH: width in pixels of the logo
- COMPANY_LOGO_POSITION: position of where the logo will be on the page. Below are the options (default TopLeft):
  - TopLeft
  - TopCenter
  - TopRight
  - BottomLeft
  - BottomCenter
  - BottomRight

## Editing the Variables
To edit these variables:
- If using docker-compose.yml, then set the geomarker.frontiers.web enviornment variables in the override file appropriately.
- If using Docker Desktop, when running an image, make sure and include the enviornment variables in the optional settings.
- If using the Docker CLI, then run `docker run -env COMPANY_NAME=Company ...`
  - Or have the variables stored in a file and run `docker run --env-file ./env.list ...`
The GeoMarker.Frontiers.GeoCode docker build installs the ASP .NET core runtime on the image with apt-get and curl. 

## Deployment

Create a Linux Virtual Machine - https://learn.microsoft.com/en-us/azure/virtual-machines/linux/quick-create-portal?tabs=ubuntu

Create a Azure Container Registry - https://learn.microsoft.com/en-us/azure/container-registry/container-registry-get-started-portal?tabs=azure-cli

Run azure-pipeline-{environment}.yaml pipeline to deploy docker images to Azure Container Registry (ACR).

SSH into Linux Vm and run ubuntu-20.04-with-docker.sh script.

Add the following firewall setting to the virtual machine.  

Under Settings > Networking tab: Add inbound port rule by filling source as IP and destination ports as 50001,50003,50006,50008,50010,50002,50004,50005,50007,50009. 