# SDF Reader (.NET Framework 4.8)

This is a .NET Framework 4.8 console application that reads SQL Server Compact Edition (SDF) files and exports their contents to JSON format.

## Purpose

The main .NET 8 application cannot read SDF files directly due to SQL CE compatibility issues. This external process serves as a bridge to extract data from SDF files.

## Building

### Windows (Visual Studio/MSBuild)
1. Open Command Prompt in this directory
2. Run `build.bat`

### Manual Build
1. Restore packages: `nuget restore packages.config -PackagesDirectory ..\packages`
2. Build: `msbuild SdfReader.csproj /p:Configuration=Debug`

## Usage

```bash
SdfReader.exe "path\to\file.sdf"
```

The program outputs JSON to stdout containing all tables from the SDF file.

## Requirements

- .NET Framework 4.8
- SQL Server Compact Edition runtime (included via NuGet package)
- Windows environment

## Output Format

```json
{
  "TableName1": [
    { "Column1": "Value1", "Column2": "Value2" },
    // ... more rows
  ],
  "TableName2": [
    // ... table data
  ]
}
```

## Integration

The main .NET 8 application calls this executable via `Process.Start()` and parses the JSON output to import data into the main SQLite database.