using PdfProcessingService.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using Vintasoft.Imaging.Pdf;
using Vintasoft.Imaging.Pdf.Tree;
using Vintasoft.Imaging.Text;

namespace PdfProcessingService.Pdf
{
    public class VSUtil
    {
        public static string GetTextFromAnchorText(string pdfPath, int pageIndex, string anchorText, float width, int startIndex = 0)
        {
            using var document = new PdfDocument(pdfPath);

            // Encuentra el texto anchor en la página
            var anchor = FindTextOnPdfPage(document, pageIndex, anchorText, startIndex);

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
                Y = anchor.Rectangle.Top + anchor.Rectangle.Height,   // Mantiene la misma altura y posición vertical que el anchor
                Width = width,                                        // Utiliza el ancho proporcionado como parámetro
                Height = anchor.Rectangle.Height                      // Mantiene la misma altura que el anchor
            };

            // Busca el texto dentro de la región especificada a la derecha del anchor
            var textRegion = GetTextSubregion(document, pageIndex, searchRectangle);

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
        public static string GetFullText(string pdfPath)
        {
            using var pdfDocument = new PdfDocument(pdfPath);
            var textBuilder = new StringBuilder();

            for (int i = 0; i < pdfDocument.Pages.Count; i++)
            {
                var page = pdfDocument.Pages[i];
                textBuilder.Append(page.TextRegion.ToString());
            }

            return textBuilder.ToString();
        }

        // --------------------------------------------------------------------------------------------------------
        // Busca texto en una región específica de la página usando coordenadas
        // --------------------------------------------------------------------------------------------------------
        public static TextRegion FindTextInRectangle(PdfDocument document, int pageIndex, SearchRectangle searchRectangle, int startIndex = 0)
        {
            var subregion = GetTextSubregion(document, pageIndex, searchRectangle);
            return subregion.FindText(searchRectangle.Expression, ref startIndex, false);
        }

        // --------------------------------------------------------------------------------------------------------
        // Busca texto en una región específica de la página usando coordenadas
        // --------------------------------------------------------------------------------------------------------
        public static TextRegion FindTextInRectangle(PdfDocument document, int pageIndex, RectangleF rectangle, string expression, int startIndex = 0)
        {
            var subregion = GetTextSubregion(document, pageIndex, rectangle);
            return subregion.FindText(expression, ref startIndex, false);
        }

        // --------------------------------------------------------------------------------------------------------
        // Busca un patrón regex en una región específica de la página usando coordenadas
        // --------------------------------------------------------------------------------------------------------
        public static TextRegion FindRegexInRectangle(PdfDocument document, int pageIndex, SearchRectangle searchRectangle)
        {
            var regexSearchEngine = TextSearchEngine.Create(CreateRegex(searchRectangle.Expression!, true));
            var subregion = GetTextSubregion(document, pageIndex, searchRectangle);

            int startIndex = 0;
            return subregion.FindText(regexSearchEngine, ref startIndex, false);
        }

        public static TextRegion FindTextOnPdfPage(string pdfPath, int pageIndex, string textToFind, int startIndex = 0)
        {
            using var document = new PdfDocument(pdfPath);
            return document.Pages[pageIndex].TextRegion.FindText(textToFind, ref startIndex, false);
        }

        // --------------------------------------------------------------------------------------------------------
        // Busca texto en toda la página
        // --------------------------------------------------------------------------------------------------------
        public static TextRegion FindTextOnPdfPage(PdfDocument document, int pageIndex, string textToFind, int startIndex = 0)
        {
            return document.Pages[pageIndex].TextRegion.FindText(textToFind, ref startIndex, false);
        }

        // --------------------------------------------------------------------------------------------------------
        // Busca un patrón regex en toda la página
        // --------------------------------------------------------------------------------------------------------
        public static TextRegion FindRegexOnPdfPage(PdfDocument document, int pageIndex, string regex, int startIndex = 0)
        {
            var regexSearchEngine = TextSearchEngine.Create(CreateRegex(regex, true));
            return document.Pages[pageIndex].TextRegion.FindText(regexSearchEngine, ref startIndex, false);
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
        public static TextRegion GetTextSubregion(PdfDocument document, int pageIndex, SearchRectangle searchRectangle)
        {
            return document.Pages[pageIndex].TextRegion.GetSubregion(
                new RectangleF(searchRectangle.Left, searchRectangle.Top, searchRectangle.Width, searchRectangle.Height),
                TextSelectionMode.Rectangle
            );
        }

        // --------------------------------------------------------------------------------------------------------
        // Método auxiliar para obtener una subregión de texto en una página del PDF
        // --------------------------------------------------------------------------------------------------------
        public static TextRegion GetTextSubregion(PdfDocument document, int pageIndex, RectangleF rectangle)
        {
            return document.Pages[pageIndex].TextRegion.GetSubregion(
                rectangle,
                TextSelectionMode.Rectangle
            );
        }

        public static string GetTextFromAnchorText(string pdfPath, int pageNumber, string anchorText)
        {

            using PdfDocument pdfDocument = new PdfDocument(pdfPath);
            PdfPage page = pdfDocument.Pages[pageNumber];
            TextRegion sourceRegion = page.TextRegion;

            int startindex = 0;
            TextRegion textRegion = sourceRegion.FindText(anchorText, ref startindex, false);

            if (textRegion != null)
            {
                // Console.WriteLine("Text found: " + textRegion.ToString());
                // Console.WriteLine($"Left: {textRegion.Rectangle.Left}, Top: {textRegion.Rectangle.Top}, Width: {textRegion.Rectangle.Width}, Height: {textRegion.Rectangle.Height}");

                float x0 = textRegion.Rectangle.X;
                float y0 = textRegion.Rectangle.Y;
                float x1 = textRegion.Rectangle.X + textRegion.Rectangle.Width;
                float y1 = textRegion.Rectangle.Y + textRegion.Rectangle.Height;

                RectangleF lineRect = new RectangleF(x1, (y0 + y1) / 2, page.MediaBox.Width - x1, 0);
                TextRegion textRegionSearch = sourceRegion.GetSubregion(lineRect, TextSelectionMode.Rectangle);

                // Console.WriteLine("Text found: " + textRegionSearch.ToString());
                // Console.WriteLine($"Left: {textRegionSearch.Rectangle.Left}, Top: {textRegionSearch.Rectangle.Top}, Width: {textRegionSearch.Rectangle.Width}, Height: {textRegionSearch.Rectangle.Height}");

                return textRegionSearch.TextContent;
            }
            else
            {
                Console.WriteLine("Text not found");
                return string.Empty;
            }
        }
    }
}
