using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using PdfUtil.Components;
using Vintasoft.Imaging.Pdf;
using Vintasoft.Imaging.Text;

namespace PdfUtil
{
    public class VSUtil
    {
        public VSUtil()
        {
            VintasoftConfigLoader.InitializeLicenses();
            Vintasoft.Imaging.Drawing.SkiaSharp.SkiaSharpDrawingFactory.SetAsDefault();
        }

        public static async Task<string> GetTextFromAnchorTextAsync(string pdfPath, int pageIndex, string anchorText, float width, int startIndex = 0)
        {
            using var document = new PdfDocument(pdfPath);

            // Encuentra el texto anchor en la página
            var anchor = await FindTextOnPdfPageAsync(document, pageIndex, anchorText, startIndex);

            if (anchor == null)
            {
                Console.WriteLine("Anchor text not found.");
                return string.Empty;
            }

            Console.WriteLine($"Left: {anchor.Rectangle.Left}, Top: {anchor.Rectangle.Top}, Width: {anchor.Rectangle.Width}, Height: {anchor.Rectangle.Height}");

            // Dado que el origen de coordenadas es el extremo inferior izquierdo,
            // se calcula la región a la derecha del texto anchor:
            var searchRectangle = new RectangleF
            {
                X = anchor.Rectangle.Left + anchor.Rectangle.Width,  // Comienza justo después del anchor (a la derecha)
                Y = anchor.Rectangle.Top + anchor.Rectangle.Height,                             // Mantiene la misma altura y posición vertical que el anchor
                Width = width,                                          // Utiliza el ancho proporcionado como parámetro
                Height = anchor.Rectangle.Height                 // Mantiene la misma altura que el anchor
            };

            // Busca el texto dentro de la región especificada a la derecha del anchor
            var textRegion = await VSUtil.GetTextSubregionAsync(document, pageIndex, searchRectangle);

            if (textRegion == null)
            {
                Console.WriteLine("No text found in the specified region to the right of the anchor.");
                return string.Empty;
            }

            Console.WriteLine($"Found text in the specified region: {textRegion.TextContent}");

            return textRegion.TextContent;
        }


        // --------------------------------------------------------------------------------------------------------
        // Obtiene el texto completo de un archivo PDF
        // --------------------------------------------------------------------------------------------------------
        public static async Task<string> GetFullTextAsync(string pdfPath)
        {
            using var pdfDocument = new PdfDocument(pdfPath);
            var textBuilder = new StringBuilder();

            for (int i = 0; i < pdfDocument.Pages.Count; i++)
            {
                var page = pdfDocument.Pages[i];
                textBuilder.Append(await Task.Run(() => page.TextRegion.ToString()));
            }

            return textBuilder.ToString();
        }

        // --------------------------------------------------------------------------------------------------------
        // Busca texto en una región específica de la página usando coordenadas
        // --------------------------------------------------------------------------------------------------------
        public static async Task<TextRegion> FindTextInRectangleAsync(PdfDocument document,
                                                                      int pageIndex,
                                                                      SearchRectangle searchRectangle,
                                                                      int startIndex = 0)
        {
            var subregion = await GetTextSubregionAsync(document, pageIndex, searchRectangle);
            return await Task.Run(() => subregion.FindText(searchRectangle.Expression, ref startIndex, false));
        }

        // --------------------------------------------------------------------------------------------------------
        // Busca texto en una región específica de la página usando coordenadas
        // --------------------------------------------------------------------------------------------------------
        public static async Task<TextRegion> FindTextInRectangleAsync(PdfDocument document, int pageIndex, RectangleF rectangle, string expression, int startIndex = 0)
        {
            var subregion = await GetTextSubregionAsync(document, pageIndex, rectangle);
            return await Task.Run(() => subregion.FindText(expression, ref startIndex, false));
        }

        // --------------------------------------------------------------------------------------------------------
        // Busca un patrón regex en una región específica de la página usando coordenadas
        // --------------------------------------------------------------------------------------------------------
        public static async Task<TextRegion> FindRegexInRectangleAsync(PdfDocument document, int pageIndex, SearchRectangle searchRectangle)
        {
            var regexSearchEngine = TextSearchEngine.Create(CreateRegex(searchRectangle.Expression!, true));
            var subregion = await GetTextSubregionAsync(document, pageIndex, searchRectangle);

            int startIndex = 0;
            return await Task.Run(() => subregion.FindText(regexSearchEngine, ref startIndex, false));
        }

        public static async Task<TextRegion> FindTextOnPdfPageAsync(string pdfPath, int pageIndex, string textToFind, int startIndex = 0)
        {
            using var document = new PdfDocument(pdfPath);
            return await Task.Run(() => document.Pages[pageIndex].TextRegion.FindText(textToFind, ref startIndex, false));
        }

        // --------------------------------------------------------------------------------------------------------
        // Busca texto en toda la página
        // --------------------------------------------------------------------------------------------------------
        public static async Task<TextRegion> FindTextOnPdfPageAsync(PdfDocument document, int pageIndex, string textToFind, int startIndex = 0)
        {
            return await Task.Run(() => document.Pages[pageIndex].TextRegion.FindText(textToFind, ref startIndex, false));
        }

        // --------------------------------------------------------------------------------------------------------
        // Busca un patrón regex en toda la página
        // --------------------------------------------------------------------------------------------------------
        public static async Task<TextRegion> FindRegexOnPdfPageAsync(PdfDocument document, int pageIndex, string regex, int startIndex = 0)
        {
            var regexSearchEngine = TextSearchEngine.Create(CreateRegex(regex, true));
            return await Task.Run(() => document.Pages[pageIndex].TextRegion.FindText(regexSearchEngine, ref startIndex, false));
        }

        // --------------------------------------------------------------------------------------------------------
        // Método auxiliar para crear un objeto Regex
        // --------------------------------------------------------------------------------------------------------
        private static Regex CreateRegex(string text, bool matchCase)
        {
            var options = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
            return new Regex(text, options);
        }

        // --------------------------------------------------------------------------------------------------------
        // Método auxiliar para obtener una subregión de texto en una página del PDF
        // --------------------------------------------------------------------------------------------------------
        public static async Task<TextRegion> GetTextSubregionAsync(PdfDocument document, int pageIndex, SearchRectangle searchRectangle)
        {
            return await Task.Run(() => document.Pages[pageIndex].TextRegion.GetSubregion(
                new System.Drawing.RectangleF(searchRectangle.Left, searchRectangle.Top, searchRectangle.Width, searchRectangle.Height),
                TextSelectionMode.Rectangle
            ));
        }

        // --------------------------------------------------------------------------------------------------------
        // Método auxiliar para obtener una subregión de texto en una página del PDF
        // --------------------------------------------------------------------------------------------------------
        public static async Task<TextRegion> GetTextSubregionAsync(PdfDocument document, int pageIndex, RectangleF rectangle)
        {
            return await Task.Run(() => document.Pages[pageIndex].TextRegion.GetSubregion(
                rectangle,
                TextSelectionMode.Rectangle
            ));
        }
    }
}
