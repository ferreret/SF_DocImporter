---
name: dotnet-build
description: Compila la solución SF_Solution.sln y analiza errores de compilación
disable-model-invocation: true
---

# Dotnet Build

Compila la solución completa y analiza los resultados.

## Instrucciones

1. Ejecuta la compilación:
```bash
dotnet build SF_Solution.sln 2>&1
```

2. Si hay errores:
   - Lee los archivos fuente referenciados en los errores
   - Analiza la causa raíz de cada error
   - Propón correcciones concretas
   - Aplica las correcciones si el usuario lo aprueba

3. Si hay warnings:
   - Lista los warnings agrupados por proyecto
   - Sugiere correcciones solo si el usuario lo pide

4. Si compila correctamente:
   - Informa del éxito y el tiempo de compilación

## Proyectos de la solución

- **PdfProcessingService** - Worker Service (net8.0)
- **GestorExpedientesWpf** - WPF App (net8.0-windows)
- **GestorRemesasWpf** - WPF App (net8.0-windows)
- **LibUtil** (LibCommon) - Librería compartida (net8.0)
- **LibDataExtractor** - Extracción PDF con VintaSoft (net8.0)
- **LibWin** - Integración Windream COM (net8.0)
- **PdfUtil** - Utilidades PDF (net8.0)
- **PdfConsoleConfig** - Consola de configuración (net8.0)

## Dependencias externas que pueden causar errores

- **VintaSoft Imaging SDK**: DLLs locales referenciadas, no NuGet
- **Windream COM Libraries**: Requiere Windream instalado en el sistema
- Si faltan estas DLLs, los errores serán de tipo "assembly not found"
