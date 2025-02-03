# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 1.3.0

### Updated

- Upgraded to .NET 8
- Upgraded NuGet packages

## 1.2.2

### Added

- Applied Microsoft Defender for Cloud Recommendations by adding apt-get upgrade command to the docker file.

## 1.2.1

### Added

- Generated new TIGER2023 Geocoder database and added to our geocoding image. 

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

- geocoder filename validations

### Changed

- Json geocoding ruby script to include ids

## 1.1.0

### Added 

- JSON endpoint and validation

## 1.0.0

### Added

- OAuth 2.0 support with the OpenIDDict GeoMarker.Frontiers.AuthServer

- api/GeoMarker/GetGeocodes: Synchronous action to process up to 300 addresses and return the tokenized addresses in a csv format with the addition of latitude and longitude.

- api/GeoMarker/StartGetGeocodesAsync: Asynchronous action to begin processing addresses and return a GUID associated to the operation that may be used to get status and results.

- api/GeoMarker/GetGeocodesStatus: Synchronous action to get the state of a geocoding process.

- api/GeoMarker/GetGeocodesResult: Synchronous action to get the result of a geocoding process as a tokenized collection of addresses in csv format witht the addtion of latitude and longitude.

- api/GeoMarker/GetGeocodeByAddress: Synchronous action to process a single address and return a json representation of the geocode result.
