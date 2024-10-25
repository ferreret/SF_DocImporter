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

        public ObservableCollection<Expediente> Expedientes { get; set; }
        public ICollectionView ExpedientesFiltrados { get; set; }
        public List<string> FacturasCargadas { get; set; }

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

        public ICommand BorrarExpedientesCommand { get; }
        public ICommand SeleccionarArchivoCommand { get; }

        public ExpedienteViewModel()
        {
            // Inicializar la colección de expedientes
            _expedientes = MockExpedienteData.GetMockExpedientes();
            CalcularIsOrphan();

            ExpedientesFiltrados = CollectionViewSource.GetDefaultView(_expedientes);

            // Inicializar RemesaFilter a Todos
            _remesaFilter = RemesaFilter.Todos;

            BorrarExpedientesCommand = new RelayCommand(BorrarExpedientes);
            SeleccionarArchivoCommand = new RelayCommand(SeleccionarArchivo);
            FiltrarExpedientes();
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

        private void FiltrarExpedientes()
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
                    FacturasCargadas = File.ReadAllLines(openFileDialog.FileName).ToList();
                    NombreArchivo = openFileDialog.FileName;
                    ColorMensajeArchivo = Brushes.Black;
                    CalcularIsFacturaCargada();
                    FiltrarExpedientes();

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
}
