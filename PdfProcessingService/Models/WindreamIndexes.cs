using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfProcessingService.Models
{
    public class WindreamIndexes
    {
        public  string?  NoAutorizacion { get; set; }
        public string? Cobertura { get; set; }
        public string? NIFMutua { get; set; }
        public string? NombrePaciente { get; set; }
        public string? DNIPaciente { get; set; }
        public DateTime FechaFactura { get; set; }
        public string? NoFactura { get; set; }
        public string? Remesa { get; set; }
        public string? CoberturaInforme { get; set; }
        public string? TipoDoc { get; set; }
    }
}
