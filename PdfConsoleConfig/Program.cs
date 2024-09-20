using PdfUtil;
using PdfUtil.Models;

Console.WriteLine("-------------------------------------------------------------");
Console.WriteLine("Vintasoft PDF templates configurator");
Console.WriteLine("-------------------------------------------------------------");

VintasoftConfigLoader.InitializeLicenses();

// Get full text form a PDF file
// string pdfPath = @"F:\Tecnomedia\SagradaFamilia\Muestras\Muestras 20240731\MSF202426971F.pdf";
// string pdfPath2 = @"F:\Tecnomedia\SagradaFamilia\Muestras\Muestras 20240731\MSF202427009F.pdf";
string pdfPath3 = @"F:\Tecnomedia\SagradaFamilia\Muestras\Muestras 20240731\MSF202427129F.pdf";

string templateFacturaPath = @"F:\Tecnomedia\SagradaFamilia\Proyecto\Templates\TemplateFactura.json";

// string fullText = await VSUtil.GetFullText(pdfPath);
// Console.WriteLine(fullText);

// Creación de la plantilla de factura
// Factura? factura1 = await TemplateManagement.ApplyFacturaTemplateAsync(pdfPath, templateFacturaPath);
// Console.WriteLine(factura1);

// Factura? factura2 = await TemplateManagement.ApplyFacturaTemplateAsync(pdfPath2, templateFacturaPath);
// Console.WriteLine(factura2);

Factura? factura3 = await TemplateManagement.ApplyFacturaTemplateAsync(pdfPath3, templateFacturaPath);
Console.WriteLine(factura3);

