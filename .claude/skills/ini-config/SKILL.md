---
name: ini-config
description: Editar y validar los archivos .ini de configuración del proyecto (PdfProcessingService, GestorExpedientesWpf, GestorRemesasWpf). Comprueba rutas, secciones y claves obligatorias antes de aplicar cambios.
disable-model-invocation: true
---

# INI Config - Configuración de aplicaciones

Skill para editar y validar los `.ini` que controlan el comportamiento de los binarios. Recuerda que el sistema **no** usa `appsettings.json`.

## Archivos cubiertos

| Archivo | App | Esquema esperado |
|---|---|---|
| `PdfProcessingService/PdfProcessingService.ini` | Worker | `ImportFolders`, `Service`, `Logging`, `Windream` |
| `GestorExpedientesWpf/GestorExpedientesWpf.ini` | WPF | `Windream`, `Mutuas` |
| `GestorRemesasWpf/GestorRemesasWpf.ini` | WPF | `Windream`, `Mutuas` |

Las copias bajo `bin/Debug/...` y `bin/Release/...` se regeneran al compilar (`CopyToOutputDirectory=Always`). **No editar las copias en `bin/`**; editar siempre el archivo fuente del proyecto.

## Esquema PdfProcessingService.ini

```ini
[ImportFolders]
Facturas=                       ; ruta de entrada
FacturasProcessed=              ; ruta de procesados
FacturasIncidencias=            ; ruta de incidencias
Autorizaciones=
AutorizacionesProcessed=
AutorizacionesIncidencias=
Informes=
InformesProcessed=
InformesIncidencias=

[Service]
DelaySeconds=30                 ; segundos antes de procesar un PDF recién copiado
PathTemplateFactura=            ; ruta absoluta a TemplateFactura.json
PathMutuas=                     ; ruta absoluta a mutuas.txt
MaxLevensthein=3                ; umbral Levenshtein para fuzzy match de mutua

[Logging]
Path=                           ; carpeta donde escribe FileLogger

[Windream]
Path=Expedientes                ; carpeta raíz dentro de Windream
ObjectType=Expediente           ; ObjectType del documento
MesesArchivo=60                 ; 0 = nunca caduca
```

## Esquema WPF (GestorExpedientesWpf / GestorRemesasWpf)

```ini
[Windream]
UnidadRed=                      ; vacío => usa \\Windream\Objects
ObjectType=Expediente

[Mutuas]
Path=                           ; ruta absoluta a mutuas.txt
```

## Validaciones obligatorias antes de guardar

1. **Todas las claves declaradas** existen (no eliminar claves; ponerlas vacías si procede).
2. **Rutas absolutas** de archivos: existen en disco (`Test-Path`). Avisa si no.
3. **Rutas de carpetas de entrada/procesados/incidencias**: existen o pueden crearse. Ofrece crear las de salida vacías; **nunca** crear las de entrada sin confirmar con el usuario.
4. **`DelaySeconds`**: entero > 0. Valor recomendado 30 — alertar si es < 5.
5. **`MaxLevensthein`**: entero entre 0 y 5. Por encima crece el riesgo de falsos positivos en mutuas.
6. **`MesesArchivo`**: entero >= 0.

## Cómo aplicar un cambio

1. Lee el archivo fuente.
2. Aplica el cambio con `Edit`.
3. Ejecuta validaciones del punto anterior.
4. Si el cambio afecta a rutas, recompila para que se actualicen las copias de `bin/`:
   ```powershell
   dotnet build SF_Solution.sln --no-restore -v q
   ```
5. Si el servicio está corriendo, recuérdaselo al usuario: hay que reiniciarlo para que coja la nueva config.

## Notas

- Los `.ini` usan codificación con caracteres especiales (`ñ`, `é`, etc.). Mantén la codificación original al escribir.
- `mutuas.txt` no se edita aquí — usa el skill `/mutuas-check`.
