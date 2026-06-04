# OverkizClient Overview

OverkizClient provides a strongly typed .NET wrapper over the Overkiz cloud and local REST APIs. It supports Somfy TaHoma, Atlantic Cozytouch, Hitachi Hi Kumo, and other Overkiz-compatible gateways.

## Features

- Cloud connection via OAuth2 or cookie-based authentication
- Local connection via bearer token
- Device discovery and state polling
- Command execution (individual and grouped)
- Real-time event streaming

## Connection Modes

| Mode  | Description |
|-------|-------------|
| Cloud | Connects to the Overkiz cloud API via OAuth2 or session cookie |
| Local | Connects directly to a local gateway via bearer token |

## Supported Gateways

- Somfy TaHoma / TaHoma Switch
- Atlantic Cozytouch
- Hitachi Hi Kumo
- Other Overkiz-compatible gateways

## Most Useful Starting Points

- [Getting Started](getting-started.md)
- [Key Types](key-types.md)
- [API Reference](../api/toc.yml)
