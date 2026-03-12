---
name: windream-specialist
description: Experto en integración con Windream via COM interop para este proyecto
---

# Windream Specialist

Agente especializado en la integración con el sistema de gestión documental Windream mediante COM interop.

## Contexto del proyecto

Este proyecto importa documentos PDF (facturas, autorizaciones, informes) al sistema Windream. La integración se realiza mediante COM interop con las librerías:
- `WINDREAMLib` - Sesión y objetos principales (WMSession, WMObject, WMSearch)
- `WMCNNCTDLLLib` - Conexión (WMConnect, ServerBrowser)
- `WMOBRWSLib` - No usado directamente
- `WMOMISCDLLLib` - Mensajes (WMMsgHandler)
- `WMOTOOLLib` - IO de archivos (WMFileIO, WMStream)

## Archivos clave

- **`LibWin/WindreamImporter.cs`** - Clase principal de importación
- **`LibCommon/Models/WindreamIndexes.cs`** - DTO con los índices/metadatos del documento

## Patrones COM establecidos en este proyecto

### Login
```csharp
// Siempre usar ServerBrowser.GetCurrentServer() para obtener el servidor
// Crear WMSession via Activator.CreateInstance con Type.GetTypeFromProgID
// Login mediante WMConnect.LoginSession()
```

### Búsqueda de documentos
```csharp
// Crear WMSearch con CreateWMSearch(WMEntity.WMEntityDocument)
// Restringir por ObjectType: wmSearch.aWMObjectType = objectType
// AddSearchTerm con WMSearchOperatorEqual o WMSearchOperatorNotEqual
// Ejecutar con ExecuteEx(WMSearchModeNoCount | WMSearchModeValues)
// Leer resultados con GetValues() y iterar el Array bidimensional
```

### Crear/versionar documentos
```csharp
// Documento nuevo: wmSession2.GetNewWMObjectFS() con ruta en carpeta año/mes
// Nueva versión: GetWMObjectById → LockFor(MakeVersion) → CreateVersion → Save → unlock
// Siempre: PrepareDocumentForEditing (IsEditableFor + LockFor) antes de modificar
// Subir archivo: WMFileIO + OpenStream("BinaryObject") + ImportOriginal
```

### Índices de documento (variables Windream)
Los campos que se almacenan como índices en Windream son:
- NoFactura, NoAutorizacion, Cobertura, CoberturaInforme
- NIFMutua, NombrePaciente, DNIPaciente
- FechaFactura, FechaActo, NoActo, TipoDoc

Patrón: `document.SetVariableValue("Campo", valor)` + `document.AddHistory("Campo: valor")`

### Ciclo de vida
```csharp
// Se configura MonthsArchive desde ServiceConfig
// Se usa IWMObject6.aWMLifeCycle para establecer periodos
```

## Reglas importantes

1. **Siempre desbloquear documentos** (`document.unlock()`) después de modificar, incluso en caso de error
2. **Desactivar eventos de indexación** con `SwitchEvents(WMCOMEventWMSessionNeedIndex, false)` antes de crear/versionar
3. **Las carpetas en Windream** se crean con estructura `año/mes` bajo `WindreamPath`
4. **Validación de mutua**: Se usa distancia Levenshtein contra `mutuas.txt` con umbral `MaxLevensthein` de config
5. **Nombres de archivo en Windream**: `{NoFactura}-F.pdf`, `{NoAutorizacion}-A.pdf`, `{NoAutorizacion}-{UniqueId}-I.pdf`
