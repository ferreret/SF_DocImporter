using System.Drawing;
using PdfUtil;
using PdfUtil.Components;
using PdfUtil.Models;
using SkiaSharp;
using Vintasoft.Imaging.Pdf;
using Vintasoft.Imaging.Pdf.Tree;
using Vintasoft.Imaging.Text;

Console.WriteLine("-------------------------------------------------------------");
Console.WriteLine("Vintasoft PDF templates configurator");
Console.WriteLine("-------------------------------------------------------------");

VintasoftConfigLoader.InitializeLicenses();

// Extracción de campos de informes usando anchor text
string[] pdfPaths =
[
    @"F:\Tecnomedia\SagradaFamilia\Muestras\Muestras 20240731\MSF202426971I.pdf",
    @"F:\Tecnomedia\SagradaFamilia\Muestras\Muestras 20240731\MSF202427009I.pdf",
    @"F:\Tecnomedia\SagradaFamilia\Muestras\Muestras 20240731\MSF202427129I.pdf"
];

string[] anchors = ["NOMBRE:", "NIF / NIE:", "COBERTURA:", "AUTORIZACIÓN:"];

foreach (string pdfPath in pdfPaths)
{
    using PdfDocument pdfDocument = new PdfDocument(pdfPath);
    PdfPage page = pdfDocument.Pages[0];
    TextRegion sourceRegion = page.TextRegion;

    foreach (string anchor in anchors)
    {
        string value = GetTextFromAnchor(sourceRegion, page, anchor);
        Console.WriteLine($"{anchor} {value}");
    }

    Console.WriteLine("-------------------------------------------------------------");
}

string GetTextFromAnchor(TextRegion sourceRegion, PdfPage page, string anchorText)
{
    int startindex = 0;
    TextRegion textRegion = sourceRegion.FindText(anchorText, ref startindex, false);

    if (textRegion == null)
    {
        Console.WriteLine($"Anchor '{anchorText}' not found");
        return string.Empty;
    }

    float x1 = textRegion.Rectangle.X + textRegion.Rectangle.Width;
    float yMid = (textRegion.Rectangle.Y + textRegion.Rectangle.Y + textRegion.Rectangle.Height) / 2;

    RectangleF lineRect = new RectangleF(x1, yMid, page.MediaBox.Width - x1, 0);
    TextRegion textRegionSearch = sourceRegion.GetSubregion(lineRect, TextSelectionMode.Rectangle);

    return textRegionSearch.TextContent;
}
