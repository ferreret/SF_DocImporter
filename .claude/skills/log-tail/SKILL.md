---
name: log-tail
description: Sigue en vivo el log del FileLogger del proyecto (por defecto en C:\Tecnomedia Sistemas\ProyectoAutorizaciones\Log). Permite filtrar por nivel y por palabra clave mientras el Worker procesa documentos.
disable-model-invocation: true
---

# Log Tail - Seguir logs en vivo

Útil cuando se ejecuta el Worker (vía `/run-worker` o como servicio) y se quiere ver en tiempo real qué está procesando, qué incidencias salta y dónde falla.

## Localizar el log activo

1. Lee `PdfProcessingService/PdfProcessingService.ini` sección `[Logging]` → `Path` (por defecto `C:\Tecnomedia Sistemas\ProyectoAutorizaciones\Log`).
2. Busca el archivo de log más reciente en esa carpeta:

```powershell
$logDir = "C:\Tecnomedia Sistemas\ProyectoAutorizaciones\Log"
Get-ChildItem $logDir -File | Sort-Object LastWriteTime -Descending | Select-Object -First 5
```

`FileLogger` (en `LibUtil/FileLogger.cs`) crea un archivo por día/sesión. El más reciente suele ser el activo.

## Seguir en vivo

```powershell
$logFile = Join-Path $logDir "<archivo-mas-reciente>.log"
Get-Content $logFile -Wait -Tail 50
```

El parámetro `-Wait` mantiene el comando enganchado y emite líneas nuevas a medida que se escriben. `Ctrl+C` para salir.

## Filtros recomendados

Sólo errores:
```powershell
Get-Content $logFile -Wait -Tail 50 | Where-Object { $_ -match 'ERROR|EXCEPTION|FALLO' }
```

Sólo un tipo de documento (por ejemplo facturas):
```powershell
Get-Content $logFile -Wait -Tail 50 | Where-Object { $_ -match 'Factura|MSF\d{9}' }
```

Sólo una mutua:
```powershell
$mutua = "FREMAP"
Get-Content $logFile -Wait -Tail 50 | Where-Object { $_ -match $mutua }
```

## Cuándo usar

- Mientras `/run-worker` está activo.
- Para diagnosticar por qué un PDF acabó en `Incidencias` (busca el nombre del archivo en el log).
- Para confirmar que un fix sobre `WindreamImporter.cs` ya no produce el error reportado.

## Notas

- Si el `Path` configurado no existe, `FileLogger` puede haber escrito a otro sitio o haber silenciado el error — comprueba también `%TEMP%` y la carpeta del ejecutable.
- En PowerShell `-Wait` requiere que el escritor cierre y reabra el archivo a veces; si parece "congelado", confirma con un `Get-Item $logFile | Select LastWriteTime`.
