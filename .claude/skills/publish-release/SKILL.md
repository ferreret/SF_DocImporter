---
name: publish-release
description: Genera artefactos de publicación con dotnet publish para PdfProcessingService (Worker), GestorExpedientesWpf y GestorRemesasWpf, incluyendo el .ini de cada uno. Útil para preparar despliegues.
disable-model-invocation: true
---

# Publish Release - Generar artefactos de despliegue

Empaqueta los tres ejecutables del proyecto con `dotnet publish` listos para copiar a la máquina destino.

## Configuración por defecto

- **Framework**: el del `.csproj` (Worker: `net8.0`, WPF: `net8.0-windows`).
- **Runtime**: `win-x64` (Windream COM sólo existe en Windows).
- **Self-contained**: `true` (no exige .NET 8 en destino).
- **PublishSingleFile**: `false` (las DLLs nativas de VintaSoft/Windream se cargan mejor sin empaquetar).
- **Configuration**: `Release`.

Si el usuario pide otra combinación (framework-dependent, single file, otro RID), respetar la petición.

## Comandos

Desde la raíz del repo:

```powershell
# 1) Build limpio
dotnet clean SF_Solution.sln -c Release
dotnet build SF_Solution.sln -c Release --no-restore

# 2) Publish Worker
dotnet publish PdfProcessingService\PdfProcessingService.csproj `
    -c Release -r win-x64 --self-contained true `
    -o publish\PdfProcessingService

# 3) Publish GestorExpedientesWpf
dotnet publish GestorExpedientesWpf\GestorExpedientesWpf.csproj `
    -c Release -r win-x64 --self-contained true `
    -o publish\GestorExpedientesWpf

# 4) Publish GestorRemesasWpf
dotnet publish GestorRemesasWpf\GestorRemesasWpf.csproj `
    -c Release -r win-x64 --self-contained true `
    -o publish\GestorRemesasWpf
```

## Verificación post-publish

Para cada `publish\<App>\` comprueba:

1. **Ejecutable** presente: `PdfProcessingService.exe` / `GestorExpedientesWpf.exe` / `GestorRemesasWpf.exe`.
2. **`.ini` correcto**: copiado por `CopyToOutputDirectory=Always` del csproj. Si falta, revisar la sección `<ItemGroup>` del `.csproj`.
3. **DLLs de Windream**: `WINDREAMLib.dll`, `WMCNNCTDLLLib.dll`, `WMOMISCDLLLib.dll`, `WMOTOOLLib.dll`. Si faltan, COM interop no funcionará en destino y hay que verificar que las referencias del `.csproj` están marcadas `Private=true`.
4. **DLLs de VintaSoft** (sólo para el Worker y PdfConsoleConfig).
5. **`mutuas.txt`** (Worker y WPF).
6. **`Templates/TemplateFactura.json`** (Worker — comprobar que se copia; si no, hay que añadirlo a CopyToOutputDirectory).

## Notas

- Windream debe estar instalado en la máquina destino para que el cliente COM funcione (las DLLs interop no traen el runtime de Windream).
- Si el destino no tiene Visual C++ Redistributable, VintaSoft puede fallar al cargar; instalar el redist x64.
- El skill `/windows-service` instala el Worker como servicio una vez publicado.
- Para reducir tamaño: probar `PublishTrimmed=false` siempre (WPF y COM interop no toleran trimming).
