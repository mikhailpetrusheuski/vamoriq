# Raw Assets

Files in this folder are copied verbatim into the app bundle (they are **not**
processed by the MAUI resizer) and can be read at runtime with
`FileSystem.OpenAppPackageFileAsync`.

## Contents

```
Raw/
└── config/
    └── appsettings.json   # API base URL, Keycloak/OIDC settings, cache and logging config
```

`appsettings.json` ships with placeholder values. Real endpoints and the
Keycloak client secret are expected to be supplied per environment (see the
root `README.md`).

## Reading a file

```csharp
using var stream = await FileSystem.OpenAppPackageFileAsync("config/appsettings.json");
using var reader = new StreamReader(stream);
var json = await reader.ReadToEndAsync();
```
