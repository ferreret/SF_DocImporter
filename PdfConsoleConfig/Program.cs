using System.Drawing;
using PdfUtil;
using PdfUtil.Components;
using PdfUtil.Models;
using SkiaSharp;
using Vintasoft.Imaging.Text;

Console.WriteLine("-------------------------------------------------------------");
Console.WriteLine("Vintasoft PDF templates configurator");
Console.WriteLine("-------------------------------------------------------------");

VintasoftConfigLoader.InitializeLicenses();

// Get full text form a PDF file
// string pdfPath = @"F:\Tecnomedia\SagradaFamilia\Muestras\Muestras 20240731\MSF202426971F.pdf";
// string pdfPath2 = @"F:\Tecnomedia\SagradaFamilia\Muestras\Muestras 20240731\MSF202427009F.pdf";
// string pdfPath3 = @"F:\Tecnomedia\SagradaFamilia\Muestras\Muestras 20240731\MSF202427129F.pdf";

// string templateFacturaPath = @"F:\Tecnomedia\SagradaFamilia\Proyecto\Templates\TemplateFactura.json";

// string fullText = await VSUtil.GetFullText(pdfPath);
// Console.WriteLine(fullText);

// Creación de la plantilla de factura
// Factura? factura1 = await TemplateManagement.ApplyFacturaTemplateAsync(pdfPath, templateFacturaPath);
// Console.WriteLine(factura1);

// Factura? factura2 = await TemplateManagement.ApplyFacturaTemplateAsync(pdfPath2, templateFacturaPath);
// Console.WriteLine(factura2);

// Factura? factura3 = await TemplateManagement.ApplyFacturaTemplateAsync(pdfPath3, templateFacturaPath);
// Console.WriteLine(factura3);

string pathPDF1 = @"F:\Tecnomedia\SagradaFamilia\Muestras\Muestras 20240731\MSF202426971I.pdf";
// string pathPDF2 = @"F:\Tecnomedia\SagradaFamilia\Muestras\Muestras 20240731\MSF202427009I.pdf";
// string pathPDF3 = @"F:\Tecnomedia\SagradaFamilia\Muestras\Muestras 20240731\MSF202427129I.pdf";

// var stopwatch = new System.Diagnostics.Stopwatch();

// stopwatch.Start();
// string fullText1 = await VSUtil.GetFullTextAsync(pathPDF1);
// Console.WriteLine(fullText1);
// stopwatch.Stop();
// Console.WriteLine($"Time taken for GetFullTextAsync(pathPDF1): {stopwatch.ElapsedMilliseconds} ms");

// stopwatch.Restart();
// string fullText2 = await VSUtil.GetFullTextAsync(pathPDF2);
// Console.WriteLine(fullText2);
// stopwatch.Stop();
// Console.WriteLine($"Time taken for GetFullTextAsync(pathPDF2): {stopwatch.ElapsedMilliseconds} ms");

// stopwatch.Restart();
// string fullText3 = await VSUtil.GetFullTextAsync(pathPDF3);
// Console.WriteLine(fullText3);
// stopwatch.Stop();
// Console.WriteLine($"Time taken for GetFullTextAsync(pathPDF3): {stopwatch.ElapsedMilliseconds} ms");

// TextRegion identityText = await VSUtil.FindTextOnPdfPageAsync(pathPDF1, 0, "Silvia Plana Artus", 0);

// Console.WriteLine($"Left: {identityText.Rectangle.Left}, Top: {identityText.Rectangle.Top}, Width: {identityText.Rectangle.Width}, Height: {identityText.Rectangle.Height}");

// TextRegion identityText2 = await VSUtil.FindTextOnPdfPageAsync(pathPDF1, 0, "O: DEU I MATA 96 7 1", 0);

// Console.WriteLine($"Left: {identityText2.Rectangle.Left}, Top: {identityText2.Rectangle.Top}, Width: {identityText2.Rectangle.Width}, Height: {identityText2.Rectangle.Height}");

// string nombre = await VSUtil.GetTextFromAnchorTextAsync(pathPDF1, 0, "NOMBRE:", 200, 0);

// Console.WriteLine(nombre);


TextRegion textRegion = await VSUtil.GetTextSubregionAsync(new Vintasoft.Imaging.Pdf.PdfDocument(pathPDF1), 0, new RectangleF(105.0f, 723.0f, 4.0f, 1.0f));
Console.WriteLine(textRegion.TextContent);



