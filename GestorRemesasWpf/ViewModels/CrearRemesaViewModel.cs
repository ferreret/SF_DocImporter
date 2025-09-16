using DocumentFormat.OpenXml.Presentation;
using GestorRemesasWpf.Models;
using LibUtil;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GestorRemesasWpf.ViewModels
{
    public class CrearRemesaViewModel : INotifyPropertyChanged
    {

        public string CarpetaDestino { get; set; }
        public List<int> DocIdsCopiados { get; private set; }

        Windream _windream;

        private List<FacturaInfo> facturasProcesadas = new List<FacturaInfo>();

        public ObservableCollection<Expediente> ExpedientesFiltrados { get; }
        public ObservableCollection<Expediente> ExpedientesTotales { get; }

        public bool CrearRemesaOk { get; set; } = false;

        public ICommand CrearRemesaCommand { get; }        

        private string _remesa;
        public string Remesa
        {
            get => _remesa;
            set
            {
                _remesa = value;
                OnPropertyChanged();
            }
        }

        private string _tipoExportacion;
        public string TipoExportacion
        {
            get => _tipoExportacion;
            set
            {
                _tipoExportacion = value;
                OnPropertyChanged();
            }
        }

        private double _progreso;
        public double Progreso
        {
            get => _progreso;
            set
            {
                _progreso = value;
                OnPropertyChanged();
            }
        }

        public CrearRemesaViewModel()
        {

        }

        public CrearRemesaViewModel(List<Expediente> expedientesFiltrados, List<Expediente> expedientesTotales)
        {
            ExpedientesFiltrados = new ObservableCollection<Expediente>(expedientesFiltrados);
            ExpedientesTotales = new ObservableCollection<Expediente>(expedientesTotales);

            CrearRemesaCommand = new RelayCommand(async () => await CrearRemesaAsync());
        }

        private async Task CrearRemesaAsync()
        {
            if (!ValidarDatosEntrada()) return;

            DocIdsCopiados = new List<int>();

            if (!SeleccionarCarpetaDestino()) return;

            if (!CrearSubcarpetaRemesa()) return;

            var numerosFacturas = ObtenerNumerosFacturasUnicos();

            Progreso = 0;
            double incrementoProgreso = 100.0 / numerosFacturas.Count;

            facturasProcesadas.Clear();

            foreach (var numeroFactura in numerosFacturas)
            {
                if (TipoExportacion.ToString().EndsWith("Adeslas"))
                {
                    await Task.Run(() => ProcesarNumeroFactura(numeroFactura));
                }
                else if (TipoExportacion.ToString().EndsWith("Sanitas"))
                {
                    await Task.Run(() => ProcesarNumeroFacturaSanitas(numeroFactura));
                }
                else if (TipoExportacion.ToString().EndsWith("Asisa"))
                {
                    //await Task.Run(() => ProcesarNumeroFacturaAsisa(numeroFactura));
                    await Task.Run(() => ProcesarNumeroFacturaMapfre(numeroFactura));
                }
                else if (TipoExportacion.ToString().EndsWith("Mapfre"))
                {
                    await Task.Run(() => ProcesarNumeroFacturaMapfre(numeroFactura));
                }
                else
                {
                    await Task.Run(() => ProcesarNumeroFacturaGeneral(numeroFactura));
                }

                Progreso += incrementoProgreso;
            }

            Progreso = 100;

            // Llamamos al método para generar el index.html
            await GenerarIndexHtmlAsync();

            _windream = new Windream();
            _windream.AsignarRemesaExpediente(DocIdsCopiados, Remesa);

            CrearRemesaOk = true;

            MessageBox.Show("Fin del proceso de creación de la remesa.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ProcesarNumeroFacturaAsisa(string numeroFactura)
        {
            try
            {
                string subcarpetaFactura = Path.Combine(CarpetaDestino, Remesa);
                if (!Directory.Exists(subcarpetaFactura))
                {
                    Directory.CreateDirectory(subcarpetaFactura);
                }

                var expedienteConFactura = ExpedientesFiltrados.FirstOrDefault(
                    expediente => expediente.NoFactura == numeroFactura && expediente.TipoDoc == "Factura");

                var facturaInfo = new FacturaInfo
                {
                    NumeroFactura = numeroFactura,
                    FechaFactura = expedienteConFactura?.FechaFactura ?? DateTime.MinValue,
                    NoAutorizacion = expedienteConFactura?.NoAutorizacion ?? string.Empty,
                    Cobertura = expedienteConFactura?.Cobertura ?? string.Empty,
                    NifMutua = expedienteConFactura?.NIFMutua ?? string.Empty,
                    NombrePaciente = expedienteConFactura?.NombrePaciente ?? string.Empty,
                    DNIPaciente = expedienteConFactura?.DNIPaciente ?? string.Empty,
                    ImporteFactura = expedienteConFactura?.ImporteFactura ?? 0
                };

                // Lista para almacenar temporalmente los archivos PDF que se van a unir
                List<string> archivosAUnir = new List<string>();
                string archivoFinal = Path.Combine(subcarpetaFactura, $"{numeroFactura}.pdf");
                
                // Primero agregamos la factura
                if (expedienteConFactura != null && File.Exists(expedienteConFactura.RutaWindream))
                {
                    archivosAUnir.Add(expedienteConFactura.RutaWindream);
                    DocIdsCopiados.Add(expedienteConFactura.DocID);
                }

                // Luego agregamos la autorización
                var expedienteConAutorizacion = ExpedientesTotales.FirstOrDefault(
                    expediente => expediente.NoFactura == numeroFactura && expediente.TipoDoc == "Autorización");

                if (expedienteConAutorizacion != null && File.Exists(expedienteConAutorizacion.RutaWindream))
                {
                    archivosAUnir.Add(expedienteConAutorizacion.RutaWindream);
                    DocIdsCopiados.Add(expedienteConAutorizacion.DocID);
                }

                // Finalmente agregamos los informes
                var expedientesConInforme = ExpedientesTotales.Where(
                    expediente => expediente.NoFactura == numeroFactura && expediente.TipoDoc == "Informe").ToList();

                foreach (var expedienteConInforme in expedientesConInforme)
                {
                    if (File.Exists(expedienteConInforme.RutaWindream))
                    {
                        archivosAUnir.Add(expedienteConInforme.RutaWindream);
                        DocIdsCopiados.Add(expedienteConInforme.DocID);
                    }
                }

                var pdfUtil = new PdfUtil();

                // Si tenemos archivos para unir, procedemos
                if (archivosAUnir.Count > 0)
                {
                    // Creamos el primer archivo PDF
                    File.Copy(archivosAUnir[0], archivoFinal, true);

                    // Unimos el resto de archivos
                    for (int i = 1; i < archivosAUnir.Count; i++)
                    {
                        pdfUtil.CopyPagesFromOnePdfDocumentToAnother(archivosAUnir[i], archivoFinal);
                    }

                    // Agregamos la información del documento para el índice HTML
                    facturaInfo.Documentos.Add(new DocumentoInfo
                    {
                        TipoDocumento = "Documentación completa",
                        NombreArchivo = Path.GetFileName(archivoFinal),
                        RutaRelativa = Path.GetFileName(archivoFinal).Replace("\\", "/")
                    });
                }

                facturasProcesadas.Add(facturaInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo procesar la factura {numeroFactura}: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProcesarNumeroFacturaMapfre(string numeroFactura)
        {
            try
            {
                string subcarpetaFactura = Path.Combine(CarpetaDestino, Remesa);
                if (!Directory.Exists(subcarpetaFactura))
                {
                    Directory.CreateDirectory(subcarpetaFactura);
                }

                var expedienteConFactura = ExpedientesFiltrados.FirstOrDefault(
                    expediente => expediente.NoFactura == numeroFactura && expediente.TipoDoc == "Factura");

                var facturaInfo = new FacturaInfo
                {
                    NumeroFactura = numeroFactura,
                    FechaFactura = expedienteConFactura?.FechaFactura ?? DateTime.MinValue,
                    NoAutorizacion = expedienteConFactura?.NoAutorizacion ?? string.Empty,
                    Cobertura = expedienteConFactura?.Cobertura ?? string.Empty,
                    NifMutua = expedienteConFactura?.NIFMutua ?? string.Empty,
                    NombrePaciente = expedienteConFactura?.NombrePaciente ?? string.Empty,
                    DNIPaciente = expedienteConFactura?.DNIPaciente ?? string.Empty,
                    ImporteFactura = expedienteConFactura?.ImporteFactura ?? 0
                };

                // Lista para almacenar temporalmente los archivos PDF que se van a unir
                List<string> archivosAUnir = new List<string>();
                string archivoFinal = Path.Combine(subcarpetaFactura, $"{numeroFactura}.pdf");

                // Primero agregamos la factura
                if (expedienteConFactura != null && File.Exists(expedienteConFactura.RutaWindream))
                {
                    archivosAUnir.Add(expedienteConFactura.RutaWindream);
                    DocIdsCopiados.Add(expedienteConFactura.DocID);
                }

                // Luego agregamos la autorización
                var expedienteConAutorizacion = ExpedientesTotales.FirstOrDefault(
                    expediente => expediente.NoFactura == numeroFactura && expediente.TipoDoc == "Autorización");

                if (expedienteConAutorizacion != null && File.Exists(expedienteConAutorizacion.RutaWindream))
                {
                    archivosAUnir.Add(expedienteConAutorizacion.RutaWindream);
                    DocIdsCopiados.Add(expedienteConAutorizacion.DocID);
                }

                // Finalmente agregamos los informes
                var expedientesConInforme = ExpedientesTotales.Where(
                    expediente => expediente.NoFactura == numeroFactura && expediente.TipoDoc == "Informe").ToList();

                foreach (var expedienteConInforme in expedientesConInforme)
                {
                    if (File.Exists(expedienteConInforme.RutaWindream))
                    {
                        archivosAUnir.Add(expedienteConInforme.RutaWindream);
                        DocIdsCopiados.Add(expedienteConInforme.DocID);
                    }
                }

                var pdfUtil = new PdfUtil();

                // Si tenemos archivos para unir, procedemos
                if (archivosAUnir.Count > 0)
                {
                    // Creamos el primer archivo PDF
                    File.Copy(archivosAUnir[0], archivoFinal, true);

                    // Unimos el resto de archivos
                    for (int i = 1; i < archivosAUnir.Count; i++)
                    {
                        pdfUtil.CopyPagesFromOnePdfDocumentToAnother(archivosAUnir[i], archivoFinal);
                    }

                    // Agregamos la información del documento para el índice HTML
                    facturaInfo.Documentos.Add(new DocumentoInfo
                    {
                        TipoDocumento = "Documentación completa",
                        NombreArchivo = Path.GetFileName(archivoFinal),
                        RutaRelativa = Path.GetFileName(archivoFinal).Replace("\\", "/")
                    });
                }

                facturasProcesadas.Add(facturaInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo procesar la factura {numeroFactura}: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProcesarNumeroFactura(string numeroFactura)
        {
            string subcarpetaFactura = Path.Combine(CarpetaDestino, Remesa, numeroFactura);
            try
            {
                if (!Directory.Exists(subcarpetaFactura))
                {
                    Directory.CreateDirectory(subcarpetaFactura);
                }

                var expedienteConFactura = ExpedientesFiltrados.FirstOrDefault(expediente => expediente.NoFactura == numeroFactura && expediente.TipoDoc == "Factura");

                // Crear objeto FacturaInfo para el índice HTML
                var facturaInfo = new FacturaInfo
                {
                    NumeroFactura = numeroFactura,
                    FechaFactura = expedienteConFactura?.FechaFactura ?? DateTime.MinValue,
                    NoAutorizacion = expedienteConFactura?.NoAutorizacion ?? string.Empty,
                    Cobertura = expedienteConFactura?.Cobertura ?? string.Empty,
                    NifMutua = expedienteConFactura?.NIFMutua ?? string.Empty,
                    NombrePaciente = expedienteConFactura?.NombrePaciente ?? string.Empty,
                    DNIPaciente = expedienteConFactura?.DNIPaciente ?? string.Empty,
                    ImporteFactura = expedienteConFactura?.ImporteFactura ?? 0
                };

                if (expedienteConFactura != null && File.Exists(expedienteConFactura.RutaWindream))
                {
                    string nombreArchivoDestino = $"{numeroFactura}F{Path.GetExtension(expedienteConFactura.RutaWindream)}";
                    string destinoArchivo = Path.Combine(subcarpetaFactura, nombreArchivoDestino);
                    File.Copy(expedienteConFactura.RutaWindream, destinoArchivo, true);
                    DocIdsCopiados.Add(expedienteConFactura.DocID);
                    
                    facturaInfo.Documentos.Add(new DocumentoInfo
                    {
                        TipoDocumento = "Factura",
                        NombreArchivo = nombreArchivoDestino,
                        RutaRelativa = Path.Combine(numeroFactura, nombreArchivoDestino).Replace("\\", "/")
                    });
                }
                else if (expedienteConFactura != null)
                {
                    MessageBox.Show($"El archivo no existe: {expedienteConFactura.RutaWindream}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                var expedienteConAutorizacion = ExpedientesTotales.FirstOrDefault(expediente => expediente.NoFactura == numeroFactura && expediente.TipoDoc == "Autorización");

                if (expedienteConAutorizacion != null && File.Exists(expedienteConAutorizacion.RutaWindream))
                {
                    string nombreArchivoDestino = $"{numeroFactura}A{Path.GetExtension(expedienteConAutorizacion.RutaWindream)}";
                    string destinoArchivo = Path.Combine(subcarpetaFactura, nombreArchivoDestino);
                    File.Copy(expedienteConAutorizacion.RutaWindream, destinoArchivo, true);
                    DocIdsCopiados.Add(expedienteConAutorizacion.DocID);
                    facturaInfo.Documentos.Add(new DocumentoInfo
                    {
                        TipoDocumento = "Autorización",
                        NombreArchivo = nombreArchivoDestino,
                        RutaRelativa = Path.Combine(numeroFactura, nombreArchivoDestino).Replace("\\", "/")
                    });
                }
                else if (expedienteConAutorizacion != null)
                {
                    MessageBox.Show($"El archivo no existe: {expedienteConAutorizacion.RutaWindream}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                var expedientesConInforme = ExpedientesTotales.Where(expediente => expediente.NoFactura == numeroFactura && expediente.TipoDoc == "Informe").ToList();
                int contadorInforme = 1;
                foreach (var expedienteConInforme in expedientesConInforme)
                {
                    if (File.Exists(expedienteConInforme.RutaWindream))
                    {
                        string nombreArchivoDestino = $"{numeroFactura}I-{contadorInforme}{Path.GetExtension(expedienteConInforme.RutaWindream)}";
                        string destinoArchivo = Path.Combine(subcarpetaFactura, nombreArchivoDestino);
                        File.Copy(expedienteConInforme.RutaWindream, destinoArchivo, true);
                        DocIdsCopiados.Add(expedienteConInforme.DocID);
                        facturaInfo.Documentos.Add(new DocumentoInfo
                        {
                            TipoDocumento = "Informe",
                            NombreArchivo = nombreArchivoDestino,
                            RutaRelativa = Path.Combine(numeroFactura, nombreArchivoDestino).Replace("\\", "/")
                        });
                        contadorInforme++;
                    }
                    else
                    {
                        MessageBox.Show($"El archivo no existe: {expedienteConInforme.RutaWindream}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                facturasProcesadas.Add(facturaInfo);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo procesar la factura {numeroFactura}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProcesarNumeroFacturaSanitas(string numeroFactura)
        {
            string subcarpetaFactura = Path.Combine(CarpetaDestino, Remesa, numeroFactura, "Factura");
            string subcarpetaDocumentos = Path.Combine(CarpetaDestino, Remesa, numeroFactura, "Documentos");
            try
            {
                if (!Directory.Exists(subcarpetaFactura))
                {
                    Directory.CreateDirectory(subcarpetaFactura);
                }

                if (!Directory.Exists(subcarpetaDocumentos))
                {
                    Directory.CreateDirectory(subcarpetaDocumentos);
                }

                var expedienteConFactura = ExpedientesFiltrados.FirstOrDefault(expediente => expediente.NoFactura == numeroFactura && expediente.TipoDoc == "Factura");

                var facturaInfo = new FacturaInfo
                {
                    NumeroFactura = numeroFactura,
                    FechaFactura = expedienteConFactura?.FechaFactura ?? DateTime.MinValue,
                    NoAutorizacion = expedienteConFactura?.NoAutorizacion ?? string.Empty,
                    Cobertura = expedienteConFactura?.Cobertura ?? string.Empty,
                    NifMutua = expedienteConFactura?.NIFMutua ?? string.Empty,
                    NombrePaciente = expedienteConFactura?.NombrePaciente ?? string.Empty,
                    DNIPaciente = expedienteConFactura?.DNIPaciente ?? string.Empty,
                    ImporteFactura = expedienteConFactura?.ImporteFactura ?? 0
                };

                if (expedienteConFactura != null && File.Exists(expedienteConFactura.RutaWindream))
                {
                    string nombreArchivoDestino = $"{numeroFactura}F{Path.GetExtension(expedienteConFactura.RutaWindream)}";
                    string destinoArchivo = Path.Combine(subcarpetaFactura, nombreArchivoDestino);
                    File.Copy(expedienteConFactura.RutaWindream, destinoArchivo, true);
                    DocIdsCopiados.Add(expedienteConFactura.DocID);
                    
                    facturaInfo.Documentos.Add(new DocumentoInfo
                    {
                        TipoDocumento = "Factura",
                        NombreArchivo = nombreArchivoDestino,
                        RutaRelativa = Path.Combine(numeroFactura, "Factura", nombreArchivoDestino).Replace("\\", "/")
                    });

                }
                else if (expedienteConFactura != null)
                {
                    MessageBox.Show($"El archivo no existe: {expedienteConFactura.RutaWindream}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                var expedienteConAutorizacion = ExpedientesTotales.FirstOrDefault(expediente => expediente.NoFactura == numeroFactura && expediente.TipoDoc == "Autorización");

                if (expedienteConAutorizacion != null && File.Exists(expedienteConAutorizacion.RutaWindream))
                {
                    string nombreArchivoDestino = $"{numeroFactura}A{Path.GetExtension(expedienteConAutorizacion.RutaWindream)}";
                    string destinoArchivo = Path.Combine(subcarpetaDocumentos, nombreArchivoDestino);
                    File.Copy(expedienteConAutorizacion.RutaWindream, destinoArchivo, true);
                    DocIdsCopiados.Add(expedienteConAutorizacion.DocID);
                    facturaInfo.Documentos.Add(new DocumentoInfo
                    {
                        TipoDocumento = "Autorización",
                        NombreArchivo = nombreArchivoDestino,
                        RutaRelativa = Path.Combine(numeroFactura, "Documentos", nombreArchivoDestino).Replace("\\", "/")
                    });
                }
                else if (expedienteConAutorizacion != null)
                {
                    MessageBox.Show($"El archivo no existe: {expedienteConAutorizacion.RutaWindream}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                var expedientesConInforme = ExpedientesTotales.Where(expediente => expediente.NoFactura == numeroFactura && expediente.TipoDoc == "Informe").ToList();
                int contadorInforme = 1;
                foreach (var expedienteConInforme in expedientesConInforme)
                {
                    if (File.Exists(expedienteConInforme.RutaWindream))
                    {
                        string nombreArchivoDestino = $"{numeroFactura}I-{contadorInforme}{Path.GetExtension(expedienteConInforme.RutaWindream)}";
                        string destinoArchivo = Path.Combine(subcarpetaDocumentos, nombreArchivoDestino);
                        File.Copy(expedienteConInforme.RutaWindream, destinoArchivo, true);
                        DocIdsCopiados.Add(expedienteConInforme.DocID);
                        facturaInfo.Documentos.Add(new DocumentoInfo
                        {
                            TipoDocumento = "Informe",
                            NombreArchivo = nombreArchivoDestino,
                            RutaRelativa = Path.Combine(numeroFactura, "Documentos", nombreArchivoDestino).Replace("\\", "/")
                        });
                        contadorInforme++;
                    }
                    else
                    {
                        MessageBox.Show($"El archivo no existe: {expedienteConInforme.RutaWindream}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                
                facturasProcesadas.Add(facturaInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo procesar la factura {numeroFactura}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProcesarNumeroFacturaGeneral(string numeroFactura)
        {
            string subcarpetaFactura = Path.Combine(CarpetaDestino, Remesa, numeroFactura);
            try
            {
                if (!Directory.Exists(subcarpetaFactura))
                {
                    Directory.CreateDirectory(subcarpetaFactura);
                }

                var expedienteConFactura = ExpedientesFiltrados.FirstOrDefault(expediente => expediente.NoFactura == numeroFactura && expediente.TipoDoc == "Factura");

                // Crear objeto FacturaInfo para el índice HTML
                var facturaInfo = new FacturaInfo
                {
                    NumeroFactura = numeroFactura,
                    FechaFactura = expedienteConFactura?.FechaFactura ?? DateTime.MinValue,
                    NoAutorizacion = expedienteConFactura?.NoAutorizacion ?? string.Empty,
                    Cobertura = expedienteConFactura?.Cobertura ?? string.Empty,
                    NifMutua = expedienteConFactura?.NIFMutua ?? string.Empty,
                    NombrePaciente = expedienteConFactura?.NombrePaciente ?? string.Empty,
                    DNIPaciente = expedienteConFactura?.DNIPaciente ?? string.Empty,
                    ImporteFactura = expedienteConFactura?.ImporteFactura ?? 0
                };

                if (expedienteConFactura != null && File.Exists(expedienteConFactura.RutaWindream))
                {
                    string nombreArchivoDestino = $"{numeroFactura}F{Path.GetExtension(expedienteConFactura.RutaWindream)}";
                    string destinoArchivo = Path.Combine(subcarpetaFactura, nombreArchivoDestino);
                    File.Copy(expedienteConFactura.RutaWindream, destinoArchivo, true);
                    DocIdsCopiados.Add(expedienteConFactura.DocID);

                    facturaInfo.Documentos.Add(new DocumentoInfo
                    {
                        TipoDocumento = "Factura",
                        NombreArchivo = nombreArchivoDestino,
                        RutaRelativa = Path.Combine(numeroFactura, nombreArchivoDestino).Replace("\\", "/")
                    });
                }
                else if (expedienteConFactura != null)
                {
                    MessageBox.Show($"El archivo no existe: {expedienteConFactura.RutaWindream}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                var expedienteConAutorizacion = ExpedientesTotales.FirstOrDefault(expediente => expediente.NoFactura == numeroFactura && expediente.TipoDoc == "Autorización");

                if (expedienteConAutorizacion != null && File.Exists(expedienteConAutorizacion.RutaWindream))
                {
                    string nombreArchivoDestino = $"{numeroFactura}A{Path.GetExtension(expedienteConAutorizacion.RutaWindream)}";
                    string destinoArchivo = Path.Combine(subcarpetaFactura, nombreArchivoDestino);
                    File.Copy(expedienteConAutorizacion.RutaWindream, destinoArchivo, true);
                    DocIdsCopiados.Add(expedienteConAutorizacion.DocID);
                    facturaInfo.Documentos.Add(new DocumentoInfo
                    {
                        TipoDocumento = "Autorización",
                        NombreArchivo = nombreArchivoDestino,
                        RutaRelativa = Path.Combine(numeroFactura, nombreArchivoDestino).Replace("\\", "/")
                    });
                }
                else if (expedienteConAutorizacion != null)
                {
                    MessageBox.Show($"El archivo no existe: {expedienteConAutorizacion.RutaWindream}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                var expedientesConInforme = ExpedientesTotales.Where(expediente => expediente.NoFactura == numeroFactura && expediente.TipoDoc == "Informe").ToList();
                int contadorInforme = 1;
                foreach (var expedienteConInforme in expedientesConInforme)
                {
                    if (File.Exists(expedienteConInforme.RutaWindream))
                    {
                        string nombreArchivoDestino = $"{numeroFactura}I-{contadorInforme}{Path.GetExtension(expedienteConInforme.RutaWindream)}";
                        string destinoArchivo = Path.Combine(subcarpetaFactura, nombreArchivoDestino);
                        File.Copy(expedienteConInforme.RutaWindream, destinoArchivo, true);
                        DocIdsCopiados.Add(expedienteConInforme.DocID);
                        facturaInfo.Documentos.Add(new DocumentoInfo
                        {
                            TipoDocumento = "Informe",
                            NombreArchivo = nombreArchivoDestino,
                            RutaRelativa = Path.Combine(numeroFactura, nombreArchivoDestino).Replace("\\", "/")
                        });
                        contadorInforme++;
                    }
                    else
                    {
                        MessageBox.Show($"El archivo no existe: {expedienteConInforme.RutaWindream}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                facturasProcesadas.Add(facturaInfo);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo procesar la factura {numeroFactura}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task GenerarIndexHtmlAsync()
        {
            string rutaPlantilla = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "index_template.html");
            string rutaIndexHtml = Path.Combine(CarpetaDestino, Remesa, "index.html");

            if (!File.Exists(rutaPlantilla))
            {
                MessageBox.Show("No se encontró la plantilla HTML.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Leer la plantilla
            string plantillaHtml = await File.ReadAllTextAsync(rutaPlantilla);

            // Generar las filas de la tabla
            StringBuilder filasTabla = new StringBuilder();
            decimal totalImporte = 0;

            foreach (var factura in facturasProcesadas)
            {
                filasTabla.AppendLine("<tr>");
                filasTabla.AppendLine($"<td>{factura.NumeroFactura}</td>");
                filasTabla.AppendLine($"<td>{factura.FechaFactura:dd/MM/yyyy}</td>");
                filasTabla.AppendLine($"<td>{factura.NoAutorizacion}</td>");
                filasTabla.AppendLine($"<td>{factura.Cobertura}</td>");
                filasTabla.AppendLine($"<td>{factura.NifMutua}</td>");
                filasTabla.AppendLine($"<td>{factura.NombrePaciente}</td>");
                filasTabla.AppendLine($"<td>{factura.DNIPaciente}</td>");
                filasTabla.AppendLine($"<td>{factura.ImporteFactura}</td>");

                // Generar los enlaces a los documentos
                filasTabla.AppendLine("<td>");
                foreach (var documento in factura.Documentos)
                {
                    filasTabla.AppendLine($"<a href=\"{documento.RutaRelativa}\" target=\"_blank\">{documento.TipoDocumento}</a><br>");
                }
                filasTabla.AppendLine("</td>");

                filasTabla.AppendLine("</tr>");

                totalImporte += factura.ImporteFactura;
            }

            // Reemplazar el marcador de posición en la plantilla
            string contenidoHtml = plantillaHtml.Replace("{{TABLE_ROWS}}", filasTabla.ToString());
            contenidoHtml = contenidoHtml.Replace("{{IMPORTE_TOTAL}}", totalImporte.ToString("C")); // Importe total

            // Guardar el archivo index.html
            await File.WriteAllTextAsync(rutaIndexHtml, contenidoHtml);
        }



        private bool ValidarDatosEntrada()
        {
            if (string.IsNullOrWhiteSpace(Remesa) || !IsValidFolderName(Remesa))
            {
                MessageBox.Show("El nombre de la remesa no es válido.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TipoExportacion))
            {
                MessageBox.Show("Debe seleccionar un tipo de exportación.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private bool SeleccionarCarpetaDestino()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                CarpetaDestino = dialog.FolderName;
                if (string.IsNullOrWhiteSpace(CarpetaDestino))
                {
                    MessageBox.Show("Debe seleccionar una carpeta de destino.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                return true;
            }

            return false;
        }

        private bool CrearSubcarpetaRemesa()
        {
            string subcarpetaRemesa = Path.Combine(CarpetaDestino, Remesa);
            try
            {
                if (!Directory.Exists(subcarpetaRemesa))
                {
                    Directory.CreateDirectory(subcarpetaRemesa);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo crear la subcarpeta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private List<string> ObtenerNumerosFacturasUnicos()
        {
            return ExpedientesFiltrados.Select(expediente => expediente.NoFactura).Distinct().ToList();
        }

        private bool IsValidFolderName(string folderName)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            string regexPattern = $"[{Regex.Escape(invalidChars)}]";
            return !Regex.IsMatch(folderName, regexPattern);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
