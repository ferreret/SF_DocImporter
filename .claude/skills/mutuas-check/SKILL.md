---
name: mutuas-check
description: Valida mutuas.txt detectando duplicados, líneas vacías, espacios sobrantes y pares de nombres con distancia Levenshtein menor o igual al umbral configurado, que provocarían falsos positivos al hacer fuzzy match.
disable-model-invocation: true
---

# Mutuas Check - Validación de mutuas.txt

`mutuas.txt` contiene los nombres válidos de aseguradoras. El extractor compara el nombre leído del PDF contra esta lista usando distancia Levenshtein con un umbral `MaxLevensthein` (por defecto 3) definido en `PdfProcessingService.ini` sección `[Service]`.

Hay tres copias del archivo. Mantenlas sincronizadas si las usas todas:
- `PdfProcessingService/mutuas.txt` (fuente del Worker)
- `GestorExpedientesWpf/mutuas.txt` (fuente de la WPF de expedientes)
- `GestorRemesasWpf/mutuas.txt` (si existe)

**Importante**: este archivo está marcado como sensible por el hook PreToolUse. Para editarlo hay que confirmar con el usuario.

## Reglas de validación

1. **Sin líneas vacías** ni líneas solo con espacios.
2. **Sin espacios al inicio o final** de ningún nombre.
3. **Sin duplicados exactos** (comparación case-insensitive).
4. **Sin pares "casi duplicados"**: para todo par `(A, B)` debe cumplirse `Levenshtein(A, B) > MaxLevensthein`. Si dos nombres están demasiado cerca, un PDF puede emparejarse con el equivocado.
5. **Longitud**: cada nombre con ≥ 4 caracteres y ≤ 120.
6. **Codificación**: el archivo debe abrir limpiamente en UTF-8; vigilar `ñ`, tildes, etc.

## Pasos

1. Lee `PdfProcessingService.ini` para obtener `MaxLevensthein`.
2. Lee `mutuas.txt`.
3. Aplica las 6 reglas. Reporta hallazgos agrupados:
   - Líneas vacías / espacios sobrantes (línea y contenido)
   - Duplicados exactos (líneas implicadas)
   - Pares con Levenshtein ≤ umbral (las dos líneas y la distancia)
   - Nombres demasiado cortos / largos
4. Si encuentras problemas, **no edites** sin confirmar. Propón el diff exacto y pide aprobación.
5. Si pides cambios y se aprueban, aplica con `Edit` y replica el cambio a las otras copias de `mutuas.txt` que estén alineadas.

## Cálculo de Levenshtein

Usa el mismo algoritmo que `LibUtil/Common.cs` (distancia clásica de edición). Para comparar contra el umbral del proyecto, redondea hacia arriba la regla del producto: si el umbral es 3, dos nombres con distancia 3 ya colisionan (el código acepta `<= MaxLevensthein` como match), así que la regla es `distancia > MaxLevensthein` para considerarse seguros.

## Ejemplo de hallazgo crítico

```
"FREMAP" vs "FREMAP "  → distancia 1, posible duplicado por espacio
"MUSAAT" vs "MUSAT"    → distancia 1, falso positivo casi seguro con umbral 3
```

## Notas

- Si el umbral está muy alto (≥ 5), señálalo: aumenta el riesgo de match incorrecto.
- Si encuentras nombres con paréntesis, comas o acrónimos, no los modifiques sin entender el contrato con los PDFs reales.
