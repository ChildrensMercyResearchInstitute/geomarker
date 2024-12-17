# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 1.3.0

### Updated

- Upgraded to .NET 8
- Upgraded NuGet packages

## 1.2.1

### Added

- Applied Microsoft Defender for Cloud Recommendations by adding apt-get upgrade command to the docker file.

## 1.2.0

### Added

- Request count for the composite request for better readability.
- Filtering and sorting for the admin page
- Validation unit tests
- User guide link in the footer

### Fixed
- Better error messages for edge cases

## 1.1.2

### Fixed

- All service images, remove single quotes from JSON content for json entrypoint script calls. 

## 1.1.1

### Added

- dep_index filename validations

## 1.1.0

### Added 

- JSON endpoint and validation

## 1.0.0

### Added

- OAuth 2.0 support with the OpenIDDict GeoMarker.Frontiers.AuthServer

- api/GeoMarker/GetDeprivationIndex: Synchronous action to get the composite deprivation index for up to 300 records.

- api/GeoMarker/StartGetDeprivationIndexesAsync: Asynchronous action to start the process to get the composite deprivation index for a collection of lat\lon records and return a GUID to be used to retrieve status and results.

- api/GeoMarker/GetDeprivationIndexesStatus: Synchronous action to get the status of a DeprivationIndex processing request.

- api/GeoMarker/GetDeprivationIndexesResult: Syncronous action to get the result of a DeprivationIndex processing request and return a csv formatted collection of records with the addtion of the composite deprivation index.
