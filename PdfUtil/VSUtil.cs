using System.Text;
using System.Text.RegularExpressions;
using PdfUtil.Components;
using Vintasoft.Imaging.Pdf;
using Vintasoft.Imaging.Text;

namespace PdfUtil;

public class VSUtil
{
    public VSUtil()
    {
        VintasoftConfigLoader.InitializeLicenses();
        Vintasoft.Imaging.Drawing.SkiaSharp.SkiaSharpDrawingFactory.SetAsDefault();
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
            textBuilder.Append(page.TextRegion.ToString());
        }

        return await Task.FromResult(textBuilder.ToString());
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
    // Busca un patrón regex en una región específica de la página usando coordenadas
    // --------------------------------------------------------------------------------------------------------
    public static TextRegion FindRegexInRectangle(PdfDocument document, int pageIndex, SearchRectangle searchRectangle)
    {
        var regexSearchEngine = TextSearchEngine.Create(CreateRegex(searchRectangle.Expression!, true));
        var subregion = GetTextSubregion(document, pageIndex, searchRectangle);

        int startIndex = 0;
        return subregion.FindText(regexSearchEngine, ref startIndex, false);
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
    private static TextRegion GetTextSubregion(PdfDocument document, int pageIndex, SearchRectangle searchRectangle)
    {
        return document.Pages[pageIndex].TextRegion.GetSubregion(
            new System.Drawing.RectangleF(searchRectangle.Left, searchRectangle.Top, searchRectangle.Width, searchRectangle.Height),
            TextSelectionMode.Rectangle
        );
    }
}
