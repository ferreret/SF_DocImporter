using System.Text.Json;
using PdfUtil;
using PdfUtil.Components;
using PdfUtil.Models;
using Vintasoft.Imaging.Pdf;
using Vintasoft.Imaging.Text;

namespace PdfUtil
{
    public class TemplateManagement
    {
        /// <summary>
        /// Aplica la plantilla de factura a un documento PDF, busca y extrae campos específicos según una definición JSON.
        /// Retorna una instancia de Factura con los campos encontrados o null si no se cumplen los requisitos mínimos.
        /// NBL - 20/09/2024
        /// </summary>
        /// <param name="pdf">Documento PDF a procesar.</param>
        /// <param name="jsonPath">Ruta del archivo JSON que contiene la definición del documento.</param>
        /// <returns>Una instancia de Factura o null si no se encuentran suficientes identificadores.</returns>
        public static async Task<Factura?> ApplyFacturaTemplateAsync(string pathPdf, string jsonPath)
        {
            using var pdf = new PdfDocument(pathPdf);
            var documentDefinition = await LoadDocumentDefinitionAsync(jsonPath);
            if (documentDefinition == null) return null;

            if (!CheckIdentifiers(pdf, documentDefinition)) return null;

            return ProcessFields(pdf, documentDefinition);
        }

        /// <summary>
        /// Carga asíncronamente la definición del documento desde un archivo JSON.
        /// NBL - 20/09/2024
        /// </summary>
        /// <param name="jsonPath">Ruta del archivo JSON.</param>
        /// <returns>Una instancia de DocumentDefinition o null en caso de error.</returns>
        private static async Task<DocumentDefinition?> LoadDocumentDefinitionAsync(string jsonPath)
        {
            try
            {
                string json = await File.ReadAllTextAsync(jsonPath);
                return JsonSerializer.Deserialize<DocumentDefinition>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar la definición del documento: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Verifica si se encuentran los identificadores requeridos en el documento PDF.
        /// NBL - 20/09/2024
        /// </summary>
        /// <param name="pdf">Documento PDF a procesar.</param>
        /// <param name="documentDefinition">Definición del documento que contiene los identificadores a buscar.</param>
        /// <returns>True si se encuentran suficientes identificadores, false en caso contrario.</returns>
        private static bool CheckIdentifiers(PdfDocument pdf, DocumentDefinition documentDefinition)
        {
            int identifiersFound = 0;
            foreach (var identifier in documentDefinition.Identifiers!)
            {
                if (TryFindTextInRectangle(pdf, identifier))
                    identifiersFound++;
            }

            if (identifiersFound < documentDefinition.MinIdentifiers)
            {
                Console.WriteLine("No se han encontrado suficientes identificadores");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Intenta encontrar un texto en una región específica del documento PDF.
        /// NBL - 20/09/2024
        /// </summary>
        /// <param name="pdf">Documento PDF a procesar.</param>
        /// <param name="identifier">Identificador que contiene las coordenadas y la expresión a buscar.</param>
        /// <returns>True si el texto es encontrado, false en caso contrario.</returns>
        private static bool TryFindTextInRectangle(PdfDocument pdf, SearchRectangle identifier)
        {
            var region = VSUtil.FindTextInRectangle(pdf, 0, identifier);
            if (region == null)
            {
                Console.WriteLine($"No se encontró el identificador: {identifier.Expression}");
                return false;
            }

            Console.WriteLine(region.Rectangle.ToString());
            Console.WriteLine(region.TextContent);
            return true;
        }

        /// <summary>
        /// Procesa los campos especificados en la definición del documento y los mapea a las propiedades de una instancia de Factura.
        /// NBL - 20/09/2024
        /// </summary>
        /// <param name="pdf">Documento PDF a procesar.</param>
        /// <param name="documentDefinition">Definición del documento con los campos a buscar.</param>
        /// <returns>Una instancia de Factura con los campos encontrados.</returns>
        private static Factura ProcessFields(PdfDocument pdf, DocumentDefinition documentDefinition)
        {
            var factura = new Factura();

            foreach (var field in documentDefinition.Fields!)
            {
                var region = VSUtil.FindRegexInRectangle(pdf, 0, field);
                if (region == null)
                {
                    Console.WriteLine($"No se encontró el campo: {field.Expression}");
                    continue;
                }

                Console.WriteLine(region.Rectangle.ToString());
                Console.WriteLine(region.TextContent);

                MapFieldToFactura(factura, field.Name!, region.TextContent);
            }

            return factura;
        }

        /// <summary>
        /// Mapea un campo encontrado en el PDF a su propiedad correspondiente en la instancia de Factura.
        /// NBL - 20/09/2024
        /// </summary>
        /// <param name="factura">Instancia de Factura a la que se asignarán los valores.</param>
        /// <param name="fieldName">Nombre del campo a mapear.</param>
        /// <param name="fieldValue">Valor del campo encontrado.</param>
        private static void MapFieldToFactura(Factura factura, string fieldName, string fieldValue)
        {
            switch (fieldName)
            {
                case "NoAutorizacion":
                    factura.NoAutorizacion = fieldValue;
                    break;
                case "Mutua":
                    factura.Mutua = fieldValue;
                    break;
                case "NombrePaciente":
                    factura.NombrePaciente = fieldValue;
                    break;
                case "DNIPaciente":
                    factura.DNIPaciente = fieldValue;
                    break;
                case "FechaFactura":
                    factura.FechaFactura = fieldValue;
                    break;
                case "NoFactura":
                    factura.NoFactura = fieldValue;
                    break;
                case "CIFMutua":
                    factura.CIFMutua = fieldValue;
                    break;
                default:
                    Console.WriteLine($"Campo no reconocido: {fieldName}");
                    break;
            }
        }

        /// <summary>
        /// Crea una plantilla de factura procesando un documento PDF y guardando los identificadores y campos en un archivo JSON.
        /// NBL - 20/09/2024
        /// </summary>
        /// <param name="pdfPath">Ruta del archivo PDF a procesar.</param>
        public static async Task CreateFacturaTemplateAsync(string pdfPath)
        {
            await Task.Run(() =>
            {
                var identifiers = CreateIdentifiers();
                var fields = CreateFields();
                var documentDefinition = new DocumentDefinition
                {
                    Name = "Factura",
                    MinIdentifiers = 1,
                    Identifiers = new List<SearchRectangle>(),
                    Fields = new List<SearchRectangle>()
                };

                using var pdfDocument = new PdfDocument(pdfPath);

                ProcessElements(pdfDocument, identifiers, documentDefinition.Identifiers, "identificador");
                ProcessElements(pdfDocument, fields, documentDefinition.Fields, "campo");

                SaveDocumentDefinition(pdfPath, documentDefinition);
            });
        }

        /// <summary>
        /// Procesa los elementos de un documento PDF (ya sean identificadores o campos) y los agrega a la lista objetivo.
        /// NBL - 20/09/2024
        /// </summary>
        /// <param name="pdfDocument">Documento PDF a procesar.</param>
        /// <param name="elements">Diccionario de elementos a buscar en el PDF.</param>
        /// <param name="targetList">Lista de rectángulos de búsqueda donde se agregan los resultados.</param>
        /// <param name="elementType">Tipo de elemento a buscar (identificador o campo).</param>
        private static void ProcessElements(PdfDocument pdfDocument, Dictionary<string, string> elements, List<SearchRectangle> targetList, string elementType)
        {
            foreach (var element in elements)
            {
                var region = VSUtil.FindTextOnPdfPage(pdfDocument, 0, element.Value);
                if (region != null)
                {
                    targetList.Add(CreateSearchRectangle(element.Key, element.Value, region));
                }
                else
                {
                    Console.WriteLine($"No se encontró el {elementType}: {element.Value}");
                }
            }
        }

        /// <summary>
        /// Crea un rectángulo de búsqueda (SearchRectangle) basado en la región de texto encontrada en el PDF.
        /// NBL - 20/09/2024
        /// </summary>
        /// <param name="key">Clave del elemento (identificador o campo).</param>
        /// <param name="value">Valor del texto a buscar.</param>
        /// <param name="region">Región de texto encontrada en el PDF.</param>
        /// <returns>Un objeto SearchRectangle con las coordenadas del texto encontrado.</returns>
        private static SearchRectangle CreateSearchRectangle(string key, string value, TextRegion region)
        {
            return new SearchRectangle
            {
                Top = (float)Math.Floor(region.Rectangle.Top),
                Left = (float)Math.Floor(region.Rectangle.Left),
                Width = (float)Math.Ceiling(region.Rectangle.Width + 1),
                Height = (float)Math.Ceiling(region.Rectangle.Height + 1),
                Expression = value,
                Name = key
            };
        }

        /// <summary>
        /// Guarda la definición del documento (identificadores y campos) en un archivo JSON.
        /// NBL - 20/09/2024
        /// </summary>
        /// <param name="pdfPath">Ruta del archivo PDF utilizado.</param>
        /// <param name="documentDefinition">Definición del documento a guardar en formato JSON.</param>
        private static void SaveDocumentDefinition(string pdfPath, DocumentDefinition documentDefinition)
        {
            var json = JsonSerializer.Serialize(documentDefinition);
            var templatePath = Path.Combine(Path.GetDirectoryName(pdfPath)!,
                                            $"TemplateFactura_{Path.GetFileNameWithoutExtension(pdfPath)}.json");

            if (File.Exists(templatePath)) File.Delete(templatePath);
            File.WriteAllText(templatePath, json);
        }

        /// <summary>
        /// Crea y devuelve un diccionario de identificadores para la plantilla de factura.
        /// NBL - 20/09/2024
        /// </summary>
        /// <returns>Un diccionario con los identificadores a buscar.</returns>
        private static Dictionary<string, string> CreateIdentifiers()
        {
            return new Dictionary<string, string>
            {
                { "Factura", "FACTURA" }
            };
        }

        /// <summary>
        /// Crea y devuelve un diccionario de campos a buscar en la plantilla de factura.
        /// NBL - 20/09/2024
        /// </summary>
        /// <returns>Un diccionario con los campos a buscar.</returns>
        private static Dictionary<string, string> CreateFields()
        {
            return new Dictionary<string, string>
            {
                { "NoAutorizacion", "B6637736" },
                { "Mutua_linea_1", "ASISTENCIA SANITARIA" },
                { "Mutua_linea_2", "COLEGIAL" },
                { "NombrePaciente", "PLANA ARTUS SILVIA" },
                { "DNIPaciente", "35108756S" },
                { "FechaFactura", "25/7/2024" },
                { "NoFactura", "MSF202426971" },
                { "CIFMutua", "A08169526" }
            };
        }
    }
}
