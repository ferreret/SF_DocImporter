using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibUtil.Models
{
    public class WindreamIndexes
    {
        public string? NoAutorizacion { get; set; }
        public string? Cobertura { get; set; }
        public string? NIFMutua { get; set; }
        public string? NombrePaciente { get; set; }
        public string? DNIPaciente { get; set; }
        public DateTime? FechaFactura { get; set; }
        public string? NoFactura { get; set; }
        public string? Remesa { get; set; }
        public string? CoberturaInforme { get; set; }
        public TipoDocumento? TipoDoc { get; set; }

        // Override ToString method to return a string representation of the object
        public override string ToString()
        {
            return $"NoAutorizacion: {NoAutorizacion}, Cobertura: {Cobertura}, NIFMutua: {NIFMutua}, NombrePaciente: {NombrePaciente}, DNIPaciente: {DNIPaciente}, FechaFactura: {FechaFactura}, NoFactura: {NoFactura}, Remesa: {Remesa}, CoberturaInforme: {CoberturaInforme}, TipoDoc: {TipoDoc}";
        }
    }

    public enum TipoDocumento
    {
        Autorización,
        Factura,
        Informe
    }
}
