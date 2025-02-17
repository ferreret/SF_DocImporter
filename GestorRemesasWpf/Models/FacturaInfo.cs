using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestorRemesasWpf.Models
{
    public class FacturaInfo
    {
        public string? NumeroFactura { get; set; }
        public DateTime FechaFactura { get; set; }
        public string? NoAutorizacion { get; set; }
        public string? Cobertura { get; set; }
        public string? NifMutua { get; set; }
        public string? NombrePaciente { get; set; }
        public string? DNIPaciente { get; set; }
        public List<DocumentoInfo> Documentos { get; set; } = new List<DocumentoInfo>();
        public decimal ImporteFactura { get; set; }
    }

    public class DocumentoInfo
    {
        public string? TipoDocumento { get; set; } // Por ejemplo: "Factura", "Informe", "Autorización"
        public string? NombreArchivo { get; set; } // Nombre del archivo en la carpeta
        public string? RutaRelativa { get; set; } // Ruta relativa desde el index.html
    }
}
