using GestorRemesasWpf.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace GestorRemesasWpf.Mock
{
    public static class MockExpedienteData
    {
        public static ObservableCollection<Expediente> GetMockExpedientes()
        {
            var expedientes = new ObservableCollection<Expediente>();

            #region "Ejemplo 1, tenemos el triplete de Autorización, Informe y Factura, sin codigo de remesa"
            // 
            expedientes.Add(new Expediente
            {                
                NombrePaciente = $"Filomeno Arias",
                DNIPaciente = "34897416A",
                FechaCreacion = DateTime.Now.AddDays(-10),
                FechaFactura = DateTime.Now.AddDays(-10),
                NoAutorizacion = "1",
                Cobertura = "CATSALUT",
                NIFMutua = "NIF123456789A",
                NoFactura = $"MSI10001",
                Remesa = "",
                CoberturaInforme = "xxx",
                TipoDoc = "Autorización",
                IsOrphan = false
            });
            expedientes.Add(new Expediente
            {
                NombrePaciente = $"Filomeno Arias",
                DNIPaciente = "34897416A",
                FechaCreacion = DateTime.Now.AddDays(-10),
                FechaFactura = DateTime.Now.AddDays(-10),
                NoAutorizacion = "1",
                Cobertura = "CATSALUT",
                NIFMutua = "NIF123456789A",
                NoFactura = $"MSI10001",
                Remesa = "",
                CoberturaInforme = "xxx",
                TipoDoc = "Factura",
                IsOrphan = false
            });
            expedientes.Add(new Expediente
            {
                NombrePaciente = $"Filomeno Arias",
                DNIPaciente = "34897416A",
                FechaCreacion = DateTime.Now.AddDays(-10),
                FechaFactura = DateTime.Now.AddDays(-10),
                NoAutorizacion = "1",
                Cobertura = "CATSALUT",
                NIFMutua = "NIF123456789A",
                NoFactura = $"MSI10001",
                Remesa = "",
                CoberturaInforme = "xxx",
                TipoDoc = "Informe",
                IsOrphan = false
            });
            #endregion

            #region "Ejemplo 2, tenemos el triplete de Autorización, Informe y Factura, con codigo de remesa"
            // 
            expedientes.Add(new Expediente
            {
                NombrePaciente = $"Filomeno Arias",
                DNIPaciente = "34897416A",
                FechaCreacion = DateTime.Now.AddDays(-10),
                FechaFactura = DateTime.Now.AddDays(-10),
                NoAutorizacion = "2",
                Cobertura = "CATSALUT",
                NIFMutua = "NIF123456789A",
                NoFactura = $"MSI10002",
                Remesa = "Rem1564",
                CoberturaInforme = "xxx",
                TipoDoc = "Autorización",
                IsOrphan = false
            });
            expedientes.Add(new Expediente
            {
                NombrePaciente = $"Filomeno Arias",
                DNIPaciente = "34897416A",
                FechaCreacion = DateTime.Now.AddDays(-10),
                FechaFactura = DateTime.Now.AddDays(-10),
                NoAutorizacion = "2",
                Cobertura = "CATSALUT",
                NIFMutua = "NIF123456789A",
                NoFactura = $"MSI10002",
                Remesa = "Rem1564",
                CoberturaInforme = "xxx",
                TipoDoc = "Factura",
                IsOrphan = false
            });
            expedientes.Add(new Expediente
            {
                NombrePaciente = $"Filomeno Arias",
                DNIPaciente = "34897416A",
                FechaCreacion = DateTime.Now.AddDays(-10),
                FechaFactura = DateTime.Now.AddDays(-10),
                NoAutorizacion = "2",
                Cobertura = "CATSALUT",
                NIFMutua = "NIF123456789A",
                NoFactura = $"MSI10002",
                Remesa = "Rem1564",
                CoberturaInforme = "xxx",
                TipoDoc = "Informe",
                IsOrphan = false
            });
            #endregion

            #region "Ejemplo 3, tenemos el triplete de Autorización, Informe y Factura, con codigo de remesa, no lo pongo en la lista de facturas"
            // 
            expedientes.Add(new Expediente
            {
                NombrePaciente = $"Mortadelo Plom",
                DNIPaciente = "64897416A",
                FechaCreacion = DateTime.Now.AddDays(-10),
                FechaFactura = DateTime.Now.AddDays(-10),
                NoAutorizacion = "3",
                Cobertura = "CATSALUT",
                NIFMutua = "NIF123456789A",
                NoFactura = $"MSI10003",
                Remesa = "Rem1565",
                CoberturaInforme = "xxx",
                TipoDoc = "Autorización",
                IsOrphan = false
            });
            expedientes.Add(new Expediente
            {
                NombrePaciente = $"Mortadelo Plom",
                DNIPaciente = "64897416A",
                FechaCreacion = DateTime.Now.AddDays(-10),
                FechaFactura = DateTime.Now.AddDays(-10),
                NoAutorizacion = "3",
                Cobertura = "CATSALUT",
                NIFMutua = "NIF123456789A",
                NoFactura = $"MSI10003",
                Remesa = "Rem1565",
                CoberturaInforme = "xxx",
                TipoDoc = "Factura",
                IsOrphan = false
            });
            expedientes.Add(new Expediente
            {
                NombrePaciente = $"Mortadelo Plom",
                DNIPaciente = "64897416A",
                FechaCreacion = DateTime.Now.AddDays(-10),
                FechaFactura = DateTime.Now.AddDays(-10),
                NoAutorizacion = "3",
                Cobertura = "CATSALUT",
                NIFMutua = "NIF123456789A",
                NoFactura = $"MSI10003",
                Remesa = "Rem1565",
                CoberturaInforme = "xxx",
                TipoDoc = "Informe",
                IsOrphan = false
            });
            #endregion

            #region "Ejemplo 4, tenemos Autorización y Factura, falta informe,  lo pongo en la lista de facturas"
            // 
            expedientes.Add(new Expediente
            {
                NombrePaciente = $"Roberto que te meto",
                DNIPaciente = "64897416A",
                FechaCreacion = DateTime.Now.AddDays(-10),
                FechaFactura = DateTime.Now.AddDays(-10),
                NoAutorizacion = "4",
                Cobertura = "CATSALUT",
                NIFMutua = "NIF123446789B",
                NoFactura = $"MSI10004",
                Remesa = "",
                CoberturaInforme = "",
                TipoDoc = "Autorización",
                IsOrphan = false
            });
            expedientes.Add(new Expediente
            {
                NombrePaciente = $"Roberto que te meto",
                DNIPaciente = "64897416A",
                FechaCreacion = DateTime.Now.AddDays(-10),
                FechaFactura = DateTime.Now.AddDays(-10),
                NoAutorizacion = "4",
                Cobertura = "CATSALUT",
                NIFMutua = "NIF123446789B",
                NoFactura = $"MSI10004",
                Remesa = "",
                CoberturaInforme = "",
                TipoDoc = "Factura",
                IsOrphan = false
            });
            #endregion

            #region "Ejemplo 5, tenemos Informe y Factura, falta autorizacion,  lo pongo en la lista de facturas"

            expedientes.Add(new Expediente
            {
                NombrePaciente = $"Paleo paleo",
                DNIPaciente = "6489f3246A",
                FechaCreacion = DateTime.Now.AddDays(-10),
                FechaFactura = DateTime.Now.AddDays(-10),
                NoAutorizacion = "",
                Cobertura = "CATSALUT",
                NIFMutua = "NIF123489489B",
                NoFactura = $"MSI10006",
                Remesa = "",
                CoberturaInforme = "bobobobobob",
                TipoDoc = "Informe",
                IsOrphan = false
            });

            expedientes.Add(new Expediente
            {
                NombrePaciente = $"Paleo paleo",
                DNIPaciente = "6489f3246A",
                FechaCreacion = DateTime.Now.AddDays(-10),
                FechaFactura = DateTime.Now.AddDays(-10),
                NoAutorizacion = "",
                Cobertura = "CATSALUT",
                NIFMutua = "NIF123489489B",
                NoFactura = $"MSI10006",
                Remesa = "",
                CoberturaInforme = "bobobobobob",
                TipoDoc = "Factura",
                IsOrphan = false
            });

            #endregion

            #region "Ejemplo 6, Añado una autorización simple, solo tiene informado el número de autorización"
            expedientes.Add(new Expediente
                {                    
                    FechaCreacion = DateTime.Now.AddDays(-10),             
                    NoAutorizacion = "6",                 
                    TipoDoc = "Autorización",
                    IsOrphan = false
                });
            #endregion

            #region "Ejemplo 7, añado un informe, solo tiene informado el nombre del paciente y el DNI"
            expedientes.Add(new Expediente
            {
                NombrePaciente = $"Paciente sin autorización",
                DNIPaciente = "12345678A",
                FechaCreacion = DateTime.Now.AddDays(-10),
                TipoDoc = "Informe",
                IsOrphan = false
            });
            #endregion


            //var random = new Random(); // Add this line to initialize the random object

            //// Crear 4 tripletes de Autorización, Informe y Factura
            //for (int i = 1; i <= 4; i++)
            //{
            //    int baseDocID = i * 3;

            //    expedientes.Add(new Expediente
            //    {
            //        DocID = baseDocID - 2,
            //        NombrePaciente = $"Paciente Fijo {i}",
            //        DNIPaciente = "DNI123A",
            //        FechaCreacion = DateTime.Now.AddDays(-10),
            //        FechaFactura = DateTime.Now.AddDays(-10),
            //        NoAutorizacion = $"AUT123{i}",
            //        Cobertura = i < 3 ? "CATSALUT" : "COMISION EUROPEA",
            //        NIFMutua = "NIF123A",
            //        NoFactura = $"FAC{i}123",
            //        Remesa = "RemesaAAA",
            //        CoberturaInforme = "Cobertura Informe A",
            //        TipoDoc = "Autorización",
            //        IsOrphan = false
            //    });

            //    expedientes.Add(new Expediente
            //    {
            //        DocID = baseDocID - 1,
            //        NombrePaciente = $"Paciente Fijo {i}",
            //        DNIPaciente = "DNI123A",
            //        FechaCreacion = DateTime.Now.AddDays(-10),
            //        FechaFactura = DateTime.Now.AddDays(-10),
            //        NoAutorizacion = $"AUT123{i}",
            //        Cobertura = i < 3 ? "CATSALUT" : "COMISION EUROPEA",
            //        NIFMutua = "NIF123A",
            //        NoFactura = $"FAC{i}123",
            //        Remesa = "RemesaAAA",
            //        CoberturaInforme = "Cobertura Informe A",
            //        TipoDoc = "Informe",
            //        IsOrphan = false
            //    });
            //    if (i != 3)
            //    {
            //        expedientes.Add(new Expediente
            //        {
            //            DocID = baseDocID,
            //            NombrePaciente = $"Paciente Fijo {i}",
            //            DNIPaciente = "DNI123A",
            //            FechaCreacion = DateTime.Now.AddDays(-10),
            //            FechaFactura = DateTime.Now.AddDays(-10),
            //            NoAutorizacion = $"AUT123{i}",
            //            Cobertura = i < 3 ? "CATSALUT" : "COMISION EUROPEA",
            //            NIFMutua = "NIF123A",
            //            NoFactura = $"FAC{i}123",
            //            Remesa = "RemesaAAA",
            //            CoberturaInforme = "Cobertura Informe A",
            //            TipoDoc = "Factura",
            //            IsOrphan = false
            //        });
            //    }                
            //}

            //// Leo todas las lineas del archivo mutuas.txt
            //var mutuas = new List<string>();
            //try
            //{
            //    mutuas.AddRange(File.ReadAllLines(@"C:\Tecnomedia Sistemas\ProyectoAutorizaciones\Fuentes\SF_DocImporter\GestorRemesasWpf\mutuas.txt"));
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error al leer el archivo mutuas.txt: {ex.Message}");
            //}

            //// Crear al menos 20 registros adicionales con diferentes valores
            //for (int i = 5; i <= 60; i++)
            //{
            //    int randomIndex = random.Next(1, 7);
            //    expedientes.Add(new Expediente
            //    {
            //        DocID = i,
            //        NombrePaciente = $"Paciente {randomIndex}",
            //        DNIPaciente = $"DNI{randomIndex}B",
            //        FechaCreacion = DateTime.Now.AddDays(-1),
            //        FechaFactura = DateTime.Now.AddDays(-1),
            //        NoAutorizacion = $"AUT{randomIndex}",
            //        Cobertura = mutuas.Count > 0 ? mutuas[random.Next(mutuas.Count)] : $"Cobertura {i}",
            //        NIFMutua = $"NIF{randomIndex}B",
            //        NoFactura = $"FAC{randomIndex}",
            //        Remesa = randomIndex <= 2 ? "" : $"Remesa {randomIndex}",
            //        CoberturaInforme = $"Cobertura Informe {randomIndex}",
            //        TipoDoc = i % 3 == 0 ? "Autorización" : i % 3 == 1 ? "Informe" : "Factura",
            //    });
            //}

            return expedientes;
        }
    }
}
