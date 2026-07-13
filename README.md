# FE Map Creator

A Fire Emblem focused map creation tool, originally published at:

https://bwdyeti.com/programs/#MapGen

## Requirements

- Windows
- .NET 10 SDK for development
- .NET 10 Desktop Runtime for the framework-dependent published application

## Build and run

```powershell
dotnet build .\FE_Map_Creator\FE_Map_Creator.sln -c Release
dotnet run --project .\FE_Map_Creator\FE_Map_Creator.csproj -c Release
```

## Test

```powershell
dotnet test .\FE_Map_Creator\FE_Map_Creator.sln -c Release
```

Run the tile-data serialization test alone:

```powershell
dotnet test .\FE_Map_Creator.Tests\FE_Map_Creator.Tests.csproj -c Release --filter "FullyQualifiedName~TileDataTests.BinaryRoundTripPreservesPriorities"
```

## Publish

```powershell
dotnet publish .\FE_Map_Creator\FE_Map_Creator.csproj -c Release
```

The framework-dependent application and its runtime assets are written to `FE_Map_Creator\bin\Release\net10.0-windows\publish\`.
