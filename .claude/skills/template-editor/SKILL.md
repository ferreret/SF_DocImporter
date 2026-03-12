---
name: template-editor
description: Editar y validar templates JSON de extracción de datos PDF (TemplateFactura.json y similares)
disable-model-invocation: true
---

# Template Editor - Extracción de datos PDF

Skill para editar y validar los templates JSON que definen las regiones de extracción de campos en documentos PDF.

## Estructura del template

Los templates se encuentran en `Templates/` y tienen esta estructura:

```json
{
  "Name": "NombreTemplate",
  "MinIdentifiers": 1,
  "Identifiers": [
    {
      "Name": "nombre_identificador",
      "Top": <pixels_desde_arriba>,
      "Left": <pixels_desde_izquierda>,
      "Width": <ancho_region>,
      "Height": <alto_region>,
      "Expression": "<regex_para_identificar_documento>"
    }
  ],
  "Fields": [
    {
      "Name": "NombreCampo",
      "Top": <pixels>,
      "Left": <pixels>,
      "Width": <pixels>,
      "Height": <pixels>,
      "Expression": "<regex_para_extraer_valor>"
    }
  ]
}
```

## Campos conocidos en TemplateFactura.json

| Campo | Descripción | Regex esperada |
|-------|-------------|----------------|
| NoAutorizacion | Número de autorización | `^B\d{7}$` |
| Mutua | Nombre de la aseguradora | `[A-ZÁÉÍÓÚÑ\s]+` |
| NombrePaciente | Nombre completo del paciente | `[A-ZÁÉÍÓÚÑ\s]+` |
| DNIPaciente | DNI/NIE del paciente | `.*` |
| FechaFactura | Fecha de la factura | Patrón dd/mm/yyyy |
| NoFactura | Número de factura | `MSF\d{9}` |
| CIFMutua | CIF de la aseguradora | `[A-Z]\d{8}` |

## Validaciones a realizar

1. **JSON válido**: Verificar que el JSON es sintácticamente correcto
2. **Campos obligatorios**: Cada Field e Identifier debe tener Name, Top, Left, Width, Height, Expression
3. **Coordenadas positivas**: Top, Left, Width, Height deben ser >= 0
4. **Regex válida**: Expression debe ser una regex compilable en C#
5. **MinIdentifiers**: Debe ser <= al número de Identifiers definidos

## Cómo probar cambios

Ejecutar PdfConsoleConfig contra un PDF de ejemplo:
```bash
dotnet run --project PdfConsoleConfig
```

Esto aplica el template al PDF y muestra los campos extraídos por consola.
