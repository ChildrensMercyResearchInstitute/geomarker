#!/bin/bash

#Update the apt package index and install packages to allow apt to use a repository over HTTPS:
sudo apt-get update
sudo apt-get install \
    ca-certificates \
    curl \
    gnupg \
    lsb-release

#Add Docker’s official GPG key
sudo mkdir -m 0755 -p /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg

#Use the following command to set up the repository:
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt-get update

#login to azure container registry
sudo docker login scmrigeomarkercontainerregistry.azurecr.io
#Username: scmrigeomarkercontainerregistry
#Password:
#WARNING! Your password will be stored unencrypted in /root/.docker/config.json.
#Configure a credential helper to remove this warning. See
#https://docs.docker.com/engine/reference/commandline/login/#credentials-store

sudo apt-get update

sudo mkdir DockerCompose

# Copy docker compose to the dockercompose directory

sudo docker compose build

# Copy SSL certs to the following folder. 

sudo cp sgeomarkercentraluscloudappazurecom.pfx /ASP.NET/Https

sudo cp sgeomarkercentraluscloudappazurecom.pfx /etc/ssl/certs

sudo chmod 755 /etc/ssl/certs/sgeomarkercentraluscloudappazurecom.pfx

# Copy docker overridden file to the dockercompose directory
# Update docker overriden file with ssl certificate and secret

sudo docker compose up -d


