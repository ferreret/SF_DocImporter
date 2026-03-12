# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run

This is a C# .NET 8.0 solution (`SF_Solution.sln`) built with MSBuild/Visual Studio 2022.

```bash
# Build entire solution
dotnet build SF_Solution.sln

# Build a specific project
dotnet build PdfProcessingService/PdfProcessingService.csproj

# Run the worker service (main application)
dotnet run --project PdfProcessingService

# Run the WPF apps
dotnet run --project GestorExpedientesWpf
dotnet run --project GestorRemesasWpf

# Run console utilities
dotnet run --project PdfConsoleConfig
```

No test projects exist in this solution. No CI/CD pipelines are configured.

## Architecture

The system automates PDF document processing for Sagrada Familia, importing documents into a Windream enterprise document management system. It handles three document types: invoices (Facturas), authorizations (Autorizaciones), and reports (Informes).

### Core Workflow

1. **PdfProcessingService** (Worker Service) monitors input folders for new PDFs
2. **LibDataExtractor** extracts metadata from PDFs using VintaSoft Imaging SDK
3. **LibWin** imports documents into Windream via COM interop
4. Processed files are archived into dated folders

### Project Dependency Graph

```
PdfProcessingService (Windows Service - orchestrator)
├── LibDataExtractor (PDF metadata extraction via VintaSoft SDK)
│   └── LibUtil
├── LibWin (Windream COM interop)
│   └── LibUtil
└── LibUtil (logging, config, utilities)

GestorExpedientesWpf (WPF - case file management UI)
└── LibUtil

GestorRemesasWpf (WPF - invoice batch management UI)
└── LibUtil

PdfConsoleConfig (CLI - PDF template testing)
└── PdfUtil (VintaSoft utilities)
```

### Key Classes

- **`PdfProcessingService/Worker.cs`** — BackgroundService that orchestrates the full pipeline: folder monitoring, extraction, validation, import, and archival.
- **`LibDataExtractor/MetaDataExtractor.cs`** — Dispatches PDF extraction by document type (`ProcessFactura`, `ProcessAutorizacion`, `ProcessInforme`). Extracts invoice numbers, dates, patient data, costs, etc.
- **`LibDataExtractor/VSUtil.cs`** — VintaSoft utility layer for text extraction, text search with coordinates, and mutua (insurance company) name lookup.
- **`LibWin/WindreamImporter.cs`** — Handles Windream document creation/versioning via COM. Uses `BuscarDocumentos` for duplicate detection.
- **`LibUtil/Common.cs`** — Levenshtein distance for fuzzy mutua name matching, unique ID generation.
- **`LibUtil/FileLogger.cs`** — File-based logging used throughout.
- **`LibUtil/ServiceConfig.cs` / `IniFile.cs`** — Configuration from INI files.

### Configuration

- **`PdfProcessingService/PdfProcessingService.ini`** — Main service config: input folder paths, processing delays, template paths, Windream connection, logging settings.
- **`Templates/TemplateFactura.json`** — JSON template defining field extraction regions for invoices.
- **`PdfProcessingService/mutuas.txt`** — List of valid insurance company names for validation.
- **`GestorExpedientesWpf/GestorExpedientesWpf.ini`** and **`GestorRemesasWpf/GestorRemesasWpf.ini`** — WPF app configs.

## Language and Conventions

- Language: C# with .NET 8.0 (WPF projects target net8.0-windows)
- All user-facing text, comments, variable names, and log messages are in **Spanish**
- External SDKs: VintaSoft Imaging (local DLL references), Windream COM libraries, ClosedXML for Excel, WebView2 for embedded web content
- Configuration uses INI files (not appsettings.json) for application-specific settings
- Models are in `LibCommon/Models/` (WindreamIndexes, Factura, DocumentDefinition)
