# Vamoriq

Daily missions for people new to a city — visit a market, start a café
conversation, find a quiet corner of a park. One small task a day, with a streak.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9-512BD4)](https://dotnet.microsoft.com/)
[![MAUI](https://img.shields.io/badge/MAUI-Android%20%7C%20iOS-512BD4)](https://learn.microsoft.com/dotnet/maui/)
[![Stars](https://img.shields.io/github/stars/mikhailpetrusheuski/vamoriq?style=social)](https://github.com/mikhailpetrusheuski/vamoriq/stargazers)

Cross-platform **.NET MAUI** sample (Android / iOS): Keycloak/OIDC, GraphQL, offline SQLite, AI personalization, 7 languages.

Missions come from a curated, localized library and are optionally
**personalized** by a backend service using the user's city and how many days
they've been going. Completing a mission captures a short proof (a note and/or
a photo) that stays on the device.

> This repository is a portfolio sample. Backend endpoints, the Keycloak client
> secret and the iOS signing identity have been replaced with placeholders — see
> [Configuration](#configuration).

## Features

- **Daily missions** across four categories (Explore, Social, Communicate, Fun)
  and three difficulty tiers, drawn from a curated library.
- **AI personalization** — a curated mission is adapted to the user's city and
  day number via a GraphQL backend, with graceful fallback to the base mission.
- **Proof capture** — text and an optional photo, stored locally.
- **Streaks & progress** — local history, current/best streak, completion stats.
- **Offline-first** — missions, proofs and streaks persist in local SQLite;
  auth tokens are validated opportunistically when a connection is available.
- **Authentication** — OpenID Connect (Keycloak) via the system browser, with
  silent token refresh.
- **Localization** — English, German, Spanish, French, Italian, Polish, Russian.
- **Theming** — light / dark, driven by `AppThemeBinding` and a theme manager.
- **Consent gate** — legal consent (privacy / terms) required before first use.
- **In-app billing** hook for a premium tier (`Plugin.InAppBilling`).

## Architecture

MVVM (`CommunityToolkit.Mvvm`) with constructor dependency injection wired up in
[`MauiProgram.cs`](Vamoriq/MauiProgram.cs).

| Project | Responsibility |
|---|---|
| `Vamoriq` | MAUI app — Shell navigation, XAML views, view models, value converters, platform code |
| `Vamoriq.Core` | Shared building blocks (`BaseViewModel`) |
| `Vamoriq.Models` | Domain models and enums (`Mission`, `CuratedMission`, `Proof`, `User`) |
| `Vamoriq.Services` | Service interfaces and implementations — auth, Keycloak/OIDC, GraphQL client, mission + streak + curated-library services, AI personalization, caching, localization, prompt provider, JWT and logging utilities |

Cross-cutting pieces:

- **Resilience** — `Polly` retry / circuit-breaker policies on the HTTP client
  (`Vamoriq/Services/PollyPolicies.cs`).
- **Curated data** — the mission library ships as JSON in
  `Vamoriq.Services/Data/missions_newcity.*.json` (one file per locale).
- **Config** — `Microsoft.Extensions.Configuration` from a bundled
  `appsettings.json`, with an optional writable override in the app data
  directory and environment-variable overrides.

## Getting started

### Prerequisites

- .NET SDK 9.0 or newer
- MAUI workloads: `dotnet workload install maui-android maui-ios`
  (`maui-ios` and device builds require macOS + Xcode)
- Android SDK (API 21+) for Android builds

### Configuration

`Vamoriq/Resources/Raw/config/appsettings.json` is committed with placeholders:

```json
{
  "ApiSettings": { "BaseUrl": "https://api.example.com" },
  "Keycloak": {
    "Authority": "https://auth.example.com/realms/vamoriq",
    "ClientId": "vamoriq",
    "ClientSecret": "REPLACE_WITH_YOUR_KEYCLOAK_CLIENT_SECRET",
    "RedirectUri": "vamoriq://callback"
  }
}
```

Point `BaseUrl` and `Authority` at your own gateway / Keycloak realm and supply
the client secret. Values can also be overridden with environment variables
(e.g. `Keycloak__ClientSecret`) or by writing an `appsettings.json` into the
app data directory at runtime.

For iOS release builds, set your own signing identity in
[`Vamoriq/Vamoriq.csproj`](Vamoriq/Vamoriq.csproj) (`CodesignKey`,
`CodesignProvision`).

### Build & run

```bash
# Restore + build (Android)
dotnet build Vamoriq/Vamoriq.csproj -f net9.0-android

# Run on a connected Android device / emulator
dotnet build Vamoriq/Vamoriq.csproj -t:Run -f net9.0-android

# iOS (macOS only)
dotnet build Vamoriq/Vamoriq.csproj -f net9.0-ios
```

## Localization

UI strings live in `Vamoriq/Resources/Localization/AppResources.*.resx` and are
resolved through a markup extension (`{loc:Localize Key}`) plus a localization
service that raises change notifications so the UI updates live when the
language changes. Curated missions carry their own per-locale translations in
the JSON data files.

## License

[MIT](LICENSE) © 2026 Mikhail Petrusheuski
