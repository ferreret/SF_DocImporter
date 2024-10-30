using ClosedXML.Excel;
using GestorRemesasWpf.Mock;
using GestorRemesasWpf.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace GestorRemesasWpf.ViewModels
{
    public enum RemesaFilter
    {
        Todos,
        SinRemesa,
        ConRemesa
    }

    public class ExpedienteViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Expediente> _expedientes;
        private string _mutuaSeleccionada;
        private string _filtroMutua;
        private RemesaFilter _remesaFilter;
        private bool _isOrphanFilter;
        private string _nombreArchivo;
        private Brush _colorMensajeArchivo;
        private bool _isFacturaFilter = true;
        private bool _mostrarSoloFacturasDelArchivo;
        private DataGrid _dataGrid;
        private bool _aplicarFiltroFechas;  

        private Expediente? _selectedExpediente;

        public ObservableCollection<Expediente> Expedientes { get; set; }

        private ICollectionView _expedientesFiltrados;
        
        public ICollectionView ExpedientesFiltrados
        {
            get => _expedientesFiltrados;
            set
            {
                _expedientesFiltrados = value;
                OnPropertyChanged(nameof(ExpedientesFiltrados));        
            }
        }

        public List<string> FacturasCargadas { get; set; }
        private bool _isBusy;

        private readonly Windream _windream;
        private DateTime _fechaInicio;
        private DateTime _fechaFin;

        public string MutuaSeleccionada
        {
            get => _mutuaSeleccionada;
            set
            {
                _mutuaSeleccionada = value;
                OnPropertyChanged(nameof(MutuaSeleccionada));
                FiltrarExpedientes();
            }
        }

        public string FiltroMutua
        {
            get => _filtroMutua;
            set
            {
                _filtroMutua = value;
                OnPropertyChanged(nameof(FiltroMutua));
                FiltrarExpedientes();
            }
        }

        public RemesaFilter RemesaFilter
        {
            get => _remesaFilter;
            set
            {
                _remesaFilter = value;
                OnPropertyChanged(nameof(RemesaFilter));
                FiltrarExpedientes();
            }
        }

        public bool IsOrphanFilter
        {
            get => _isOrphanFilter;
            set
            {
                _isOrphanFilter = value;
                OnPropertyChanged(nameof(IsOrphanFilter));
                FiltrarExpedientes();
            }
        }

        public string NombreArchivo
        {
            get => _nombreArchivo;
            set
            {
                _nombreArchivo = value;
                OnPropertyChanged(nameof(NombreArchivo));
            }
        }

        public Brush ColorMensajeArchivo
        {
            get => _colorMensajeArchivo;
            set
            {
                _colorMensajeArchivo = value;
                OnPropertyChanged(nameof(ColorMensajeArchivo));
            }
        }

        public bool IsFacturaFilter
        {
            get => _isFacturaFilter;
            set
            {
                _isFacturaFilter = value;
                OnPropertyChanged(nameof(IsFacturaFilter));
                FiltrarExpedientes();
            }
        }

        public bool MostrarSoloFacturasDelArchivo
        {
            get => _mostrarSoloFacturasDelArchivo;
            set
            {
                _mostrarSoloFacturasDelArchivo = value;
                OnPropertyChanged(nameof(MostrarSoloFacturasDelArchivo));
                FiltrarExpedientes();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        public string SelectedExpedienteUrl => SelectedExpediente?.RutaWindream ?? "about:blank";

        public Expediente? SelectedExpediente
        {
            get => _selectedExpediente;
            set
            {
                _selectedExpediente = value;
                OnPropertyChanged(nameof(SelectedExpediente));
                OnPropertyChanged(nameof(SelectedExpedienteUrl));
            }
        }

        public bool AplicarFiltroFechas
        {
            get => _aplicarFiltroFechas;
            set
            {
                _aplicarFiltroFechas = value;
                OnPropertyChanged(nameof(AplicarFiltroFechas));
            }
        }

        public DateTime FechaInicio
        {
            get => _fechaInicio;
            set
            {
                _fechaInicio = value;
                OnPropertyChanged(nameof(FechaInicio));
            }
        }

        public DateTime FechaFin
        {
            get => _fechaFin;
            set
            {
                _fechaFin = value;
                OnPropertyChanged(nameof(FechaFin));
            }
        }


        public ICommand BorrarExpedientesCommand { get; }
        public ICommand SeleccionarArchivoCommand { get; }
        public ICommand ExportarCsvCommand { get; }
        public ICommand ExportarExcelCommand { get; }
        public ICommand ActualizarCommand { get; }
        public ICommand CrearRemesaCommand { get; }

        private async void Actualizar()
        {
            IsBusy = true;

            try
            {
                await Task.Run(() => CargarExpedientes(AplicarFiltroFechas, FechaInicio, FechaFin));
                FiltrarExpedientes();
            }
            finally
            {
                IsBusy = false;
            }
        }

        public ExpedienteViewModel()
        {

        }

        public ExpedienteViewModel(DataGrid dataGrid)
        {

            _windream = new Windream();

            FechaInicio = DateTime.Now;
            FechaFin = DateTime.Now;

            _dataGrid = dataGrid;

            // Inicializar la colección de expedientes
            //_expedientes = MockExpedienteData.GetMockExpedientes();
            //CalcularIsOrphan();

            // Esto tiene que ir en actualizar
           

            // Inicializar RemesaFilter a Todos
            _remesaFilter = RemesaFilter.Todos;

            BorrarExpedientesCommand = new RelayCommand(BorrarExpedientes);
            SeleccionarArchivoCommand = new RelayCommand(SeleccionarArchivo);
            ExportarCsvCommand = new RelayCommand(ExportarCsv);
            ExportarExcelCommand = new RelayCommand(ExportarExcel);
            ActualizarCommand = new RelayCommand(Actualizar);
            CrearRemesaCommand = new RelayCommand(AbrirCrearRemesa);
            //FiltrarExpedientes();

        }

        private void AbrirCrearRemesa()
        {
            // Validar que ExpedientesFiltrados y _expedientes tengan datos
            // Validar que ExpedientesFiltrados y _expedientes no sean nulos
            if (_expedientes == null || ExpedientesFiltrados == null)
            {
                MessageBox.Show("Los expedientes no están cargados correctamente.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!_expedientes.Any() || !ExpedientesFiltrados.Cast<Expediente>().Any())
            {
                MessageBox.Show("No hay expedientes disponibles para crear una remesa.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ventanaCrearRemesa = new CrearRemesaWindow
            {
                DataContext = new CrearRemesaViewModel(ExpedientesFiltrados.Cast<Expediente>().ToList(), _expedientes.ToList()),
                Owner = Application.Current.MainWindow
            };
            ventanaCrearRemesa.ShowDialog();
            
            if (ventanaCrearRemesa.DataContext is CrearRemesaViewModel crearRemesaViewModel)
            {
                if (crearRemesaViewModel.CrearRemesaOk)
                {
                    // Actualizar la lista de expedientes
                    Actualizar();
                    NombreArchivo = "";
                    FacturasCargadas.Clear();
                }
            }
        }

        public void CargarExpedientes(bool aplicarFiltroFechas, DateTime fechaInicio, DateTime fechaFin)
        {
            var expedientes = _windream.GetExpedientes(aplicarFiltroFechas, fechaInicio, fechaFin);
            _expedientes = expedientes;
            CalcularIsOrphan();            
            ExpedientesFiltrados = CollectionViewSource.GetDefaultView(_expedientes);            
        }

        private void ExportarExcel()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Expedientes");

                        // Añadir encabezados
                        worksheet.Cell(1, 1).Value = "DocID";
                        worksheet.Cell(1, 2).Value = "RutaWindream";
                        worksheet.Cell(1, 3).Value = "NoAutorizacion";
                        worksheet.Cell(1, 4).Value = "FechaCreacion";
                        worksheet.Cell(1, 5).Value = "Cobertura";
                        worksheet.Cell(1, 6).Value = "NIFMutua";
                        worksheet.Cell(1, 7).Value = "NombrePaciente";
                        worksheet.Cell(1, 8).Value = "DNIPaciente";
                        worksheet.Cell(1, 9).Value = "FechaFactura";
                        worksheet.Cell(1, 10).Value = "NoFactura";
                        worksheet.Cell(1, 11).Value = "Remesa";
                        worksheet.Cell(1, 12).Value = "CoberturaInforme";
                        worksheet.Cell(1, 13).Value = "TipoDoc";
                        worksheet.Cell(1, 14).Value = "IsOrphan";
                        worksheet.Cell(1, 15).Value = "EsFacturaCargada";
                        worksheet.Cell(1, 16).Value = "EsFacturaOrphan";
                        worksheet.Cell(1, 17).Value = "EsFacturaInvalida";
                        worksheet.Cell(1, 18).Value = "FaltaInforme";
                        worksheet.Cell(1, 19).Value = "FaltaAutorizacion";

                        int row = 2;
                        foreach (Expediente expediente in ExpedientesFiltrados)
                        {
                            worksheet.Cell(row, 1).Value = expediente.DocID;
                            worksheet.Cell(row, 2).Value = expediente.RutaWindream;
                            worksheet.Cell(row, 3).Value = expediente.NoAutorizacion;
                            worksheet.Cell(row, 4).Value = expediente.FechaCreacion;
                            worksheet.Cell(row, 5).Value = expediente.Cobertura;
                            worksheet.Cell(row, 6).Value = expediente.NIFMutua;
                            worksheet.Cell(row, 7).Value = expediente.NombrePaciente;
                            worksheet.Cell(row, 8).Value = expediente.DNIPaciente;
                            worksheet.Cell(row, 9).Value = expediente.FechaFactura;
                            worksheet.Cell(row, 10).Value = expediente.NoFactura;
                            worksheet.Cell(row, 11).Value = expediente.Remesa;
                            worksheet.Cell(row, 12).Value = expediente.CoberturaInforme;
                            worksheet.Cell(row, 13).Value = expediente.TipoDoc;
                            worksheet.Cell(row, 14).Value = expediente.IsOrphan;
                            worksheet.Cell(row, 15).Value = expediente.EsFacturaCargada;
                            worksheet.Cell(row, 16).Value = expediente.EsFacturaOrphan;
                            worksheet.Cell(row, 17).Value = expediente.EsFacturaInvalida;
                            worksheet.Cell(row, 18).Value = expediente.FaltaInforme;
                            worksheet.Cell(row, 19).Value = expediente.FaltaAutorizacion;

                            // Aplicar colores basados en las condiciones del XAML
                            switch (expediente.TipoDoc)
                            {
                                case "Autorización":
                                    worksheet.Cell(row, 13).Style.Font.FontColor = XLColor.FromHtml("#dc3545");
                                    worksheet.Cell(row, 13).Style.Font.Bold = true;
                                    break;
                                case "Informe":
                                    worksheet.Cell(row, 13).Style.Font.FontColor = XLColor.FromHtml("#007bff");
                                    worksheet.Cell(row, 13).Style.Font.Bold = true;
                                    break;
                                case "Factura":
                                    worksheet.Cell(row, 13).Style.Font.FontColor = XLColor.FromHtml("#28a745");
                                    worksheet.Cell(row, 13).Style.Font.Bold = true;
                                    break;
                            }

                            if (expediente.EsFacturaCargada)
                            {
                                worksheet.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#28a745");
                                worksheet.Cell(row, 10).Style.Font.FontColor = XLColor.White;
                                worksheet.Cell(row, 10).Style.Font.Bold = true;
                            }
                            if (expediente.EsFacturaOrphan)
                            {
                                worksheet.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffc107");
                                worksheet.Cell(row, 10).Style.Font.Bold = true;
                            }
                            if (expediente.EsFacturaInvalida)
                            {
                                worksheet.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#dc3545");
                                worksheet.Cell(row, 10).Style.Font.FontColor = XLColor.White;
                                worksheet.Cell(row, 10).Style.Font.Bold = true;
                            }
                            if (expediente.EsFacturaSinAutorizacion)
                            {
                                worksheet.Cell(row, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffc107");
                                worksheet.Cell(row, 3).Style.Font.Bold = true;
                            }
                            if (expediente.EsFacturaSinInforme)
                            {
                                worksheet.Cell(row, 12).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffc107");
                                worksheet.Cell(row, 12).Style.Font.Bold = true;
                            }

                            row++;
                        }

                        workbook.SaveAs(saveFileDialog.FileName);

                        MessageBox.Show("Archivo exportado correctamente", "Exportar a Excel", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar el archivo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportarCsv()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new StreamWriter(saveFileDialog.FileName))
                    {
                        writer.WriteLine("DocID,RutaWindream,NoAutorizacion,FechaCreacion,Cobertura,NIFMutua,NombrePaciente,DNIPaciente,FechaFactura,NoFactura,Remesa,CoberturaInforme,TipoDoc,IsOrphan,EsFacturaCargada,EsFacturaOrphan,EsFacturaInvalida,FaltaInforme,FaltaAutorizacion");

                        foreach (Expediente expediente in ExpedientesFiltrados)
                        {
                            writer.WriteLine($"{expediente.DocID},{expediente.RutaWindream},{expediente.NoAutorizacion},{expediente.FechaCreacion},{expediente.Cobertura},{expediente.NIFMutua},{expediente.NombrePaciente},{expediente.DNIPaciente},{expediente.FechaFactura},{expediente.NoFactura},{expediente.Remesa},{expediente.CoberturaInforme},{expediente.TipoDoc},{expediente.IsOrphan},{expediente.EsFacturaCargada},{expediente.EsFacturaOrphan},{expediente.EsFacturaInvalida},{expediente.FaltaInforme},{expediente.FaltaAutorizacion}");
                        }

                        MessageBox.Show("Archivo exportado correctamente", "Exportar a CSV", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar el archivo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CalcularIsOrphan()
        {
            if (_expedientes == null) return;

            var expedientesPorAutorizacion = _expedientes.GroupBy(e => e.NoAutorizacion);

            foreach (var grupo in expedientesPorAutorizacion)
            {
                bool tieneAutorizacion = false;
                bool tieneFactura = false;
                bool tieneInforme = false;

                foreach (var expediente in grupo)
                {
                    if (!tieneAutorizacion && expediente.TipoDoc == "Autorización")
                        tieneAutorizacion = true;
                    if (!tieneFactura && expediente.TipoDoc == "Factura")
                        tieneFactura = true;
                    if (!tieneInforme && expediente.TipoDoc == "Informe")
                        tieneInforme = true;

                    if (tieneAutorizacion && tieneFactura && tieneInforme)
                        break;
                }

                bool isOrphan = !(tieneAutorizacion && tieneFactura && tieneInforme);
                foreach (var expediente in grupo)
                {
                    expediente.IsOrphan = isOrphan;
                    expediente.FaltaAutorizacion = !tieneAutorizacion;
                    expediente.FaltaInforme = !tieneInforme;
                }
            }
        }

        private void CalcularIsFacturaCargada()
        {
            if (_expedientes == null) return;

            foreach (var expediente in _expedientes)
            {
                expediente.EsFacturaCargada = IsFacturaCargada(expediente.NoFactura);
                expediente.EsFacturaOrphan = EsFacturaOrphan(expediente);
                expediente.EsFacturaInvalida = EsFacturaInvalida(expediente);
            }
        }

        // Modificación: Añadido método para verificar si la factura está cargada
        public bool IsFacturaCargada(string noFactura)
        {
            return FacturasCargadas?.Contains(noFactura) ?? false;
        }

        // Modificación: Añadidas propiedades para verificar el estado de la factura
        public bool EsFacturaCargada(Expediente expediente) => IsFacturaCargada(expediente.NoFactura) && string.IsNullOrEmpty(expediente.Remesa);
        public bool EsFacturaOrphan(Expediente expediente) => IsFacturaCargada(expediente.NoFactura) && expediente.IsOrphan;
        public bool EsFacturaInvalida(Expediente expediente) => IsFacturaCargada(expediente.NoFactura) && !string.IsNullOrEmpty(expediente.Remesa);

        private async void FiltrarExpedientes()
        {                        
            ExpedientesFiltrados.Filter = expediente =>
            {
                var exp = (Expediente)expediente;
                bool mutuaMatch = string.IsNullOrEmpty(_mutuaSeleccionada) || exp.Cobertura == _mutuaSeleccionada;
                bool filtroMutuaMatch = string.IsNullOrEmpty(_filtroMutua) || exp.Cobertura.Contains(_filtroMutua, StringComparison.OrdinalIgnoreCase);
                bool isOrphanMatch = !_isOrphanFilter || exp.IsOrphan;
                bool isFacturaMatch = !_isFacturaFilter || exp.TipoDoc == "Factura";
                bool remesaMatch = _remesaFilter switch
                {
                    RemesaFilter.Todos => true,
                    RemesaFilter.SinRemesa => string.IsNullOrEmpty(exp.Remesa),
                    RemesaFilter.ConRemesa => !string.IsNullOrEmpty(exp.Remesa),
                    _ => true
                };
                bool facturaArchivoMatch = !_mostrarSoloFacturasDelArchivo || (FacturasCargadas != null && FacturasCargadas.Contains(exp.NoFactura));
                
                return mutuaMatch && filtroMutuaMatch && remesaMatch && isOrphanMatch && isFacturaMatch && facturaArchivoMatch;
            };
            ExpedientesFiltrados.Refresh();            
        }

        private void BorrarExpedientes()
        {
            _expedientes.Clear();
            FiltrarExpedientes();
        }

        private void SeleccionarArchivo()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    IsBusy = true;
                    FacturasCargadas = File.ReadAllLines(openFileDialog.FileName).ToList();
                    NombreArchivo = openFileDialog.FileName;
                    ColorMensajeArchivo = Brushes.Black;
                    CalcularIsFacturaCargada();
                    FiltrarExpedientes();
                    IsBusy = false;
                    // Obtener las facturas que no están en la colección de expedientes
                    var facturasNoEncontradas = FacturasCargadas.Where(f => !_expedientes.Any(e => e.NoFactura == f)).ToList();

                    if (facturasNoEncontradas.Any())
                    {
                        MessageBox.Show("Hay facturas en el archivo que no están en la lista de expedientes", "Facturas no encontradas", MessageBoxButton.OK, MessageBoxImage.Warning);

                        // Crear un archivo temporal y escribir las facturas no encontradas
                        string tempFilePath = Path.GetTempFileName() + ".txt";
                        File.WriteAllLines(tempFilePath, facturasNoEncontradas);

                        // Abrir el archivo temporal en el bloc de notas
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "notepad.exe",
                            Arguments = tempFilePath,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    NombreArchivo = "Error al cargar el archivo";
                    ColorMensajeArchivo = Brushes.Red;
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    // Add this extension method to convert System.Windows.Media.Color to System.Drawing.Color
    public static class ColorExtensions
    {
        public static System.Drawing.Color ToDrawingColor(this System.Windows.Media.Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
