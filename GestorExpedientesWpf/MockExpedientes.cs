using GestorExpedientesWpf.Models;
using System;
using System.Collections.ObjectModel;

namespace GestorExpedientesWpf.ViewModels
{
    public static class MockExpedientes
    {
        public static ObservableCollection<Expediente> GetExpedientes()
        {
            return new ObservableCollection<Expediente>
            {
                new Expediente
                {
                    DocID = 1,
                    RutaWindream = @"C:\Users\Usuario\Documents\Expedientes\Expediente1",
                    NoAutorizacion = "123456",
                    FechaCreacion = DateTime.Now,
                    Cobertura = "Cobertura 1",
                    NIFMutua = "12345678A",
                    NombrePaciente = "Paciente 1",
                    DNIPaciente = "12345678A",
                    FechaFactura = DateTime.Now,
                    NoFactura = "123456",
                    Remesa = "Remesa 1",
                    CoberturaInforme = "Cobertura Informe 1",
                    TipoDoc = "Autorización"
                },
                new Expediente
                {
                    DocID = 2,
                    RutaWindream = @"C:\Users\Usuario\Documents\Expedientes\Expediente2",
                    NoAutorizacion = "123457",
                    FechaCreacion = DateTime.Now,
                    Cobertura = "Cobertura 2",
                    NIFMutua = "12345678B",
                    NombrePaciente = "Paciente 2",
                    DNIPaciente = "12345678B",
                    FechaFactura = DateTime.Now,
                    NoFactura = "123457",
                    Remesa = "Remesa 2",
                    CoberturaInforme = "Cobertura Informe 2",
                    TipoDoc = "Factura"
                },
                new Expediente
                {
                    DocID = 3,
                    RutaWindream = @"C:\Users\Usuario\Documents\Expedientes\Expediente3",
                    NoAutorizacion = "123458",
                    FechaCreacion = DateTime.Now,
                    Cobertura = "Cobertura 3",
                    NIFMutua = "12345678C",
                    NombrePaciente = "Paciente 3",
                    DNIPaciente = "12345678C",
                    FechaFactura = DateTime.Now,
                    NoFactura = "123458",
                    Remesa = "Remesa 3",
                    CoberturaInforme = "Cobertura Informe 3",
                    TipoDoc = "Informe"
                },
                new Expediente
                {
                    DocID = 4,
                    RutaWindream = @"C:\Users\Usuario\Documents\Expedientes\Expediente4",
                    NoAutorizacion = "123459",
                    FechaCreacion = DateTime.Now,
                    Cobertura = "Cobertura 4",
                    NIFMutua = "12345678D",
                    NombrePaciente = "Paciente 4",
                    DNIPaciente = "12345678D",
                    FechaFactura = DateTime.Now,
                    NoFactura = "123459",
                    Remesa = string.Empty,
                    CoberturaInforme = "Cobertura Informe 4",
                    TipoDoc = "Autorización"
                },
                new Expediente
                {
                    DocID = 5,
                    RutaWindream = @"C:\Users\Usuario\Documents\Expedientes\Expediente5",
                    NoAutorizacion = "123460",
                    FechaCreacion = DateTime.Now,
                    Cobertura = "Cobertura 5",
                    NIFMutua = "12345678E",
                    NombrePaciente = "Paciente 5",
                    DNIPaciente = "12345678E",
                    FechaFactura = DateTime.Now,
                    NoFactura = "123460",
                    Remesa = "Remesa 5",
                    CoberturaInforme = "Cobertura Informe 5",
                    TipoDoc = "Autorización"
                },
                new Expediente
                {
                    DocID = 6,
                    RutaWindream = @"C:\Users\Usuario\Documents\Expedientes\Expediente6",
                    NoAutorizacion = "123460",
                    FechaCreacion = DateTime.Now,
                    Cobertura = "Cobertura 6",
                    NIFMutua = "12345678F",
                    NombrePaciente = "Paciente 6",
                    DNIPaciente = "12345678F",
                    FechaFactura = DateTime.Now,
                    NoFactura = "123460",
                    Remesa = "Remesa 6",
                    CoberturaInforme = "Cobertura Informe 6",
                    TipoDoc = "Factura"
                },
                new Expediente
                {
                    DocID = 7,
                    RutaWindream = @"C:\Users\Usuario\Documents\Expedientes\Expediente7",
                    NoAutorizacion = "123460",
                    FechaCreacion = DateTime.Now,
                    Cobertura = "Cobertura 7",
                    NIFMutua = "12345678G",
                    NombrePaciente = "Paciente 7",
                    DNIPaciente = "12345678G",
                    FechaFactura = DateTime.Now,
                    NoFactura = "123460",
                    Remesa = "Remesa 7",
                    CoberturaInforme = "Cobertura Informe 7",
                    TipoDoc = "Informe"
                }
            };
        }
    }
}
