using PdfUtil;

Console.WriteLine("-------------------------------------------------------------");
Console.WriteLine("Vintasoft PDF templates configurator");
Console.WriteLine("-------------------------------------------------------------");


VSUtil vsUtil = new VSUtil();

// Get full text form a PDF file
string pdfPath = @"F:\Tecnomedia\SagradaFamilia\Factura\Ejemplo factura.pdf";
string fullText = vsUtil.GetFullText(pdfPath);
Console.WriteLine(fullText);
