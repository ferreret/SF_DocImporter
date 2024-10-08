using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibUtil.Models
{
    public class Factura
    {
        public string? NoAutorizacion { get; set; }
        public string? Mutua { get; set; }
        public string? NombrePaciente { get; set; }
        public string? DNIPaciente { get; set; }
        public string? FechaFactura { get; set; }
        public string? NoFactura { get; set; }
        public string? CIFMutua { get; set; }

        public override string ToString()
        {
            return $"Factura:\n" +
                   $"NoAutorizacion: {NoAutorizacion}\n" +
                   $"Mutua: {Mutua}\n" +
                   $"NombrePaciente: {NombrePaciente}\n" +
                   $"DNIPaciente: {DNIPaciente}\n" +
                   $"FechaFactura: {FechaFactura}\n" +
                   $"NoFactura: {NoFactura}\n" +
                   $"CIFMutua: {CIFMutua}";
        }
    }
}
