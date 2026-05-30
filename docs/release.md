# Release-Plan

## Lokaler Release-Build

```powershell
dotnet publish .\Schach.csproj -c Release -r win-x64 --self-contained false -o .\artifacts\Schach
```

## Installer-Idee

- Start mit ZIP-Release aus `artifacts/Schach`.
- Spaeter optional MSIX oder WiX-Installer.
- GitHub Releases koennen ZIP-Artefakte plus Changelog enthalten.
