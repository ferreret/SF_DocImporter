---
name: windows-service
description: Gestiona PdfProcessingService como servicio Windows con sc.exe — instalar, desinstalar, arrancar, parar y consultar estado. Requiere PowerShell elevado (administrador).
disable-model-invocation: true
---

# Windows Service - Gestión del Worker como servicio

`PdfProcessingService` está preparado para ejecutarse como servicio Windows (`Program.cs` llama a `AddWindowsService`). Este skill cubre el ciclo de vida del servicio.

## Requisitos

- **PowerShell elevado** (Run as administrator). Sin esto, `sc.exe create/delete/start/stop` falla con acceso denegado.
- Antes de instalar el servicio, **publicar** el Worker con `/publish-release` o tener el binario compilado en `bin\Release\net8.0\publish\PdfProcessingService.exe`.

## Variables sugeridas (PowerShell)

```powershell
$svcName = "PdfProcessingService"
$svcDisplay = "SF DocImporter - PDF Processing Service"
$svcBinary = "C:\Servicios\PdfProcessingService\PdfProcessingService.exe"   # Ajustar
$svcUser = "NT AUTHORITY\NetworkService"                                     # o cuenta de dominio con acceso a Windream
```

> El servicio necesita permisos para leer las carpetas de entrada, escribir en `Procesados`/`Incidencias`/`Log` y acceder a Windream. `NetworkService` puede no bastar si hay rutas UNC; en ese caso usar una cuenta de servicio con permisos explícitos.

## Instalar

```powershell
sc.exe create $svcName binPath= "`"$svcBinary`"" DisplayName= $svcDisplay start= auto obj= $svcUser
sc.exe description $svcName "Importa PDFs (facturas, autorizaciones, informes) a Windream"
sc.exe failure $svcName reset= 86400 actions= restart/60000/restart/60000/restart/60000
```

Notas:
- El espacio detrás de cada `=` es obligatorio en `sc.exe` (sintaxis legacy).
- `failure` configura reinicios automáticos: tres reintentos a 60 s cada uno, ventana de reset 24 h.

## Arrancar / parar / estado

```powershell
sc.exe start $svcName
sc.exe stop  $svcName
sc.exe query $svcName
Get-Service $svcName | Format-List Name, Status, StartType, ServiceType
```

## Reiniciar tras cambio de config

Si se edita `PdfProcessingService.ini`:

```powershell
sc.exe stop  $svcName
# Esperar hasta STOPPED
do { Start-Sleep -Seconds 1; $s = (Get-Service $svcName).Status } until ($s -eq 'Stopped')
sc.exe start $svcName
```

## Desinstalar

```powershell
sc.exe stop $svcName
sc.exe delete $svcName
```

## Diagnóstico

- Si el servicio no arranca, mirar:
  - `Visor de eventos` → `Aplicación` → fuente `PdfProcessingService` o `.NET Runtime`.
  - El log de `FileLogger` (skill `/log-tail`).
  - Permisos de la cuenta del servicio sobre las carpetas configuradas.
- Errores típicos:
  - `Error 5: Acceso denegado` → la cuenta no tiene permiso sobre la carpeta de entrada o el log.
  - `Error 1053: el servicio no respondió a tiempo` → fallo de Windream (login COM bloqueado) o ruta de `.ini` errónea.
  - `Error 1067: el proceso terminó inesperadamente` → ver Visor de eventos para la excepción.

## Notas

- El servicio se llama `PdfProcessingService` y eso lo define `Program.cs` línea 17 (`options.ServiceName = "PdfProcessingService"`). Si se cambia, hay que actualizar `$svcName` aquí.
- No instalar a la vez una sesión interactiva con `/run-worker` y el servicio: los `FileSystemWatcher` competirían por los mismos archivos.
