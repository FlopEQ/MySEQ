# MySEQ

MySEQ is a Windows solution with a .NET 10 WinForms client and a native C++ server.

## Requirements

- Current Visual Studio with MSBuild
- .NET 10 SDK for the client and tools
- MSVC build tools and Windows SDK for the server

## Build

Restore packages and build the full solution:

```powershell
dotnet build client\MySEQ.client.csproj -c Release
```

Build only the server:

```powershell
msbuild server\MySEQ.server.vcxproj /p:Configuration=Release /p:Platform=x64
```

Build the offset diff finder:

```powershell
dotnet publish tools\OffsetDiffFinder\OffsetDiffFinder.csproj -c Release -r win-x64 --self-contained false
```

The client uses `PackageReference`; `packages.config` is not used.
