# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## 1.4.1

### Added

- KU Medical Center to DriveTime
- 
## 1.4.0

### Updated

- Upgraded to .NET 8
- Upgraded NuGet packages

## 1.3.0

### Updated

- Use auth server role based access control for authorized access to pages.

### Fixed

- Custom error handler now ignores API requests, allows api layer to handle error body.

## 1.2.3

### Updated:

- Updated to OpenIddict 5
- Upgraded additional packages

## 1.2.2

### Added

- Children's Mercy Locations and KU Medical Center to DriveTime.
- Custom Error Handler.
- User Request API for SCALE users.

## 1.2.1

### Added

- Download Sample Files link in the footer

## 1.2.0

### Added

- Request count for the composite request for better readability.
- Filtering and sorting for the admin page
- Validation unit tests
- User guide link in the footer

### Fixed

- Better error messages for edge cases

## 1.1.1

### Added

- Service filename validations

### Changed

- Gateway controller to support ids on json geocoding

## 1.1.0

### Added

- JSON Gateway API endpoints and validation
- Service unavailable messages from gateway when services are down
- Swagger UI Authorization and documentation
- Administrative portal for tracing processed records and usage
- Client side form validation for GeoMarker UI

## 1.0.1

### Added

- GNU GENERAL PUBLIC LICENSE

### Changed:

- Single address error messages to be more clear
- Enforce TLS communication between web and auth in the production environment.

## 1.0.0

### Added

- OAuth 2.0 Cookie based user authentication and authorization with the OpenIDDict GeoMarker.Frontiers.AuthServer
- Multi Address GeoMarking UI
- Single Address GeoMarking UI
- DeGauss Process Chaining for Multi Address GeoMarking
- User request status persistence and processed record count metadata
- API Gateway layer for controlled exposure of underlying API's
