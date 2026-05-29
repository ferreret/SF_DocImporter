---
name: run-worker
description: Arranca PdfProcessingService en consola y observa el ciclo Entrada/Procesados/Incidencias junto con el log. Útil para probar el pipeline end-to-end sin instalar el servicio.
disable-model-invocation: true
---

# Run Worker - Pipeline end-to-end manual

Lanza el Worker Service `PdfProcessingService` en consola (no como servicio Windows) para reproducir el flujo completo de importación.

## Pre-vuelo

Antes de arrancar:

1. Verifica que existen las carpetas configuradas en `PdfProcessingService/PdfProcessingService.ini` (sección `[ImportFolders]`):
   - Entrada: `Facturas`, `Autorizaciones`, `Informes`
   - Procesados: `FacturasProcessed`, `AutorizacionesProcessed`, `InformesProcessed`
   - Incidencias: `FacturasIncidencias`, `AutorizacionesIncidencias`, `InformesIncidencias`
2. Comprueba que `PathTemplateFactura` y `PathMutuas` apuntan a archivos existentes.
3. Confirma que Windream está accesible (servicio `wmpserver` corriendo). Si no, la importación fallará en el login.
4. Avisa al usuario antes de arrancar si encuentras alguna carpeta faltante; ofrece crear las de salida vacías pero NO la de entrada (esa la controla el negocio).

## Arrancar

Desde la raíz del repositorio:

```powershell
dotnet run --project PdfProcessingService
```

El servicio queda escuchando los `FileSystemWatcher` y reaccionando al `DelaySeconds` configurado (por defecto 30 s) antes de procesar un PDF recién copiado.

Para detener: `Ctrl+C`.

## Pruebas

1. **Camino feliz**: copia un PDF de factura real en `Entrada/Facturas`. Espera el `DelaySeconds` + tiempo de procesamiento. Comprueba:
   - Aparece en `Procesados/Facturas/<año>/<mes>/`
   - Se ha creado/versionado en Windream (consulta vía UI o `BuscarDocumentos`)
   - El log no muestra errores

2. **Camino con incidencia**: copia un PDF que no case con el template. Comprueba:
   - Aparece en `Incidencias/Facturas/<año>/<mes>/`
   - El log indica la razón (template no match, mutua no encontrada, etc.)

3. **Mutua difusa**: copia un PDF con un nombre de mutua mal escrito pero cercano a uno de `mutuas.txt`. Verifica que Levenshtein lo encaja dentro del `MaxLevensthein` configurado (3 por defecto).

## Observabilidad

- Log de archivo en `[Logging] Path` (por defecto `C:\Tecnomedia Sistemas\ProyectoAutorizaciones\Log`). Considera lanzar en paralelo el skill `/log-tail`.
- La consola muestra logs del Host genérico (`Microsoft.Extensions.Hosting`).

## Notas

- Si Windream COM no está disponible, el Worker arranca pero falla al primer documento; revisa logs.
- VintaSoft requiere DLLs locales (no NuGet); si faltan, hay errores de "assembly not found".
- No usar este skill para probar como servicio Windows — para eso está `/windows-service`.
