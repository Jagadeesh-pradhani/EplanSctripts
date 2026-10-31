# EPLAN Automation

A C# and .NET-based automation framework for EPLAN Electric P8.

## Features

- EPLAN Remote API communication
- EPLAN connection management
- Macro import automation
- Project automation
- Page management
- Device and component automation
- Automated report generation

## Requirements

- Windows
- .NET SDK
- EPLAN Electric P8
- EPLAN Remote API / Extensions API

## Project Structure

- `src/` - Main application and libraries
- `tests/` - Unit and integration tests
- `scripts/` - EPLAN scripts and PowerShell utilities
- `docs/` - Documentation
- `examples/` - Usage examples
- `config/` - Configuration templates

## Getting Started

Clone the repository:

```bash
git clone <repository-url>
cd EplanAutomation
```

Build:

```bash
dotnet build
```

Run:

```bash
dotnet run --project src/EplanAutomation.App
```

## Configuration

Copy `config/appsettings.example.json`
to your local configuration file and configure
your EPLAN connection settings.

## License

To be decided.


#
```
dotnet new console -n EplanAutomation.Remote.Tests -o tests/EplanAutomation.Remote.Tests
dotnet build tests/EplanAutomation.Remote.Tests/EplanAutomation.Remote.Tests.csproj
dotnet run --project tests/EplanAutomation.Remote.Tests/EplanAutomation.Remote.Tests.csproj

```