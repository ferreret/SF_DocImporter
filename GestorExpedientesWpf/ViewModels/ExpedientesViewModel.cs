using GestorExpedientesWpf.Command;
using GestorExpedientesWpf.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace GestorExpedientesWpf.ViewModels
{
    public class ExpedientesViewModel : INotifyPropertyChanged
    {
        private readonly Windream _windream;

        private ObservableCollection<Expediente>? _expedientes;
        private ObservableCollection<Expediente>? _seleccionados;

        private bool _aplicarFiltroFechas;
        private DateTime _fechaInicio;
        private DateTime _fechaFin;

        private ICommand _actualizarCommand;
        private ICommand _addSeleccionCommand;
        private ICommand _removeSeleccionCommand;

        private bool _ignorarDocumentosConRemesa;
        private bool _mostrarSoloDocumentosHuerfanos;

        private ICollectionView _expedientesView;

        private Expediente? _selectedExpediente;
        
        public string SelectedExpedienteUrl => SelectedExpediente?.RutaWindream ?? "about:blank";

        public Expediente? SelectedExpediente
        {
            get => _selectedExpediente;
            set
            {
                _selectedExpediente = value;
                //MessageBox.Show($"Expediente seleccionado: {value?.RutaWindream}");
                OnPropertyChanged(nameof(SelectedExpediente));
                OnPropertyChanged(nameof(SelectedExpedienteUrl));
            }
        }

        public ObservableCollection<Expediente> Expedientes
        {
            get => _expedientes ?? new ObservableCollection<Expediente>();
            set
            {
                _expedientes = value;
                OnPropertyChanged(nameof(Expedientes));
                OnPropertyChanged(nameof(TotalDocumentos));
                OnPropertyChanged(nameof(AutorizacionesCount));
                OnPropertyChanged(nameof(FacturasCount));
                OnPropertyChanged(nameof(InformesCount));
            }
        }

        public ICollectionView ExpedientesView
        {
            get => _expedientesView;
            set
            {
                _expedientesView = value;
                OnPropertyChanged(nameof(ExpedientesView));
            }
        }

        public ObservableCollection<Expediente> Seleccionados
        {
            get => _seleccionados ??= new ObservableCollection<Expediente>();
            set
            {
                _seleccionados = value;
                OnPropertyChanged(nameof(Seleccionados));
            }
        }

        public int TotalDocumentos => ExpedientesView?.Cast<Expediente>().Count() ?? 0;
        public int AutorizacionesCount => ExpedientesView?.Cast<Expediente>().Count(e => e.TipoDoc == "Autorización") ?? 0;
        public int FacturasCount => ExpedientesView?.Cast<Expediente>().Count(e => e.TipoDoc == "Factura") ?? 0;
        public int InformesCount => ExpedientesView?.Cast<Expediente>().Count(e => e.TipoDoc == "Informe") ?? 0;


        public bool IgnorarDocumentosConRemesa
        {
            get => _ignorarDocumentosConRemesa;
            set
            {
                _ignorarDocumentosConRemesa = value;
                OnPropertyChanged(nameof(IgnorarDocumentosConRemesa));
                FiltrarExpedientes();
            }
        }

        public bool MostrarSoloDocumentosHuerfanos
        {
            get => _mostrarSoloDocumentosHuerfanos;
            set
            {
                _mostrarSoloDocumentosHuerfanos = value;
                OnPropertyChanged(nameof(MostrarSoloDocumentosHuerfanos));
                FiltrarExpedientes();
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

        public ExpedientesViewModel()
        {
            // No muestro los datos de los expedientes en el constructor
            // CargarExpedientes();

            _windream = new Windream();

            // Inicializo las fechas con el día de hoy
            FechaInicio = DateTime.Now;
            FechaFin = DateTime.Now;

            // Inicializo el comando
            _actualizarCommand = new RelayCommand(param => this.Actualizar(), null);
            _addSeleccionCommand = new RelayCommand<Expediente>(AgregarSeleccionado);
            _removeSeleccionCommand = new RelayCommand<Expediente>(QuitarSeleccionado);

            IgnorarDocumentosConRemesa = true;
            MostrarSoloDocumentosHuerfanos = false;
        }

        public void CargarExpedientes(bool aplicarFiltroFechas, DateTime fechaInicio, DateTime fechaFin)
        {
            // Cargar expedientes de prueba
            //var expedientes = MockExpedientes.GetExpedientes();
             var expedientes = _windream.GetExpedientes(aplicarFiltroFechas, fechaInicio, fechaFin);

            Expedientes = expedientes;
            CalcularIsOrphan();
            FiltrarExpedientes();
        }

        private void CalcularIsOrphan()
        {
            if (_expedientes == null) return;

            var expedientesPorAutorizacion = _expedientes.GroupBy(e => e.NoAutorizacion);

            foreach (var grupo in expedientesPorAutorizacion)
            {
                // Usamos HashSet para evitar recorrer varias veces la colección.
                bool tieneAutorizacion = false;
                bool tieneFactura = false;
                bool tieneInforme = false;

                // Recorremos el grupo una sola vez para verificar la presencia de los documentos.
                foreach (var expediente in grupo)
                {
                    if (!tieneAutorizacion && expediente.TipoDoc == "Autorización")
                        tieneAutorizacion = true;
                    if (!tieneFactura && expediente.TipoDoc == "Factura")
                        tieneFactura = true;
                    if (!tieneInforme && expediente.TipoDoc == "Informe")
                        tieneInforme = true;

                    // Si ya tenemos los tres, podemos romper el bucle
                    if (tieneAutorizacion && tieneFactura && tieneInforme)
                        break;
                }

                // Ahora asignamos IsOrphan para cada expediente en el grupo
                bool isOrphan = !(tieneAutorizacion && tieneFactura && tieneInforme);
                foreach (var expediente in grupo)
                {
                    expediente.IsOrphan = isOrphan;
                }
            }
        }


        private void FiltrarExpedientes()
        {
            if (_expedientes == null) return;

            var collectionView = CollectionViewSource.GetDefaultView(_expedientes);
            collectionView.Filter = e =>
            {
                var expediente = e as Expediente;
                bool pasaFiltroRemesa = !IgnorarDocumentosConRemesa || string.IsNullOrEmpty(expediente!.Remesa);
                bool pasaFiltroHuerfanos = !MostrarSoloDocumentosHuerfanos || expediente!.IsOrphan;
                return pasaFiltroRemesa && pasaFiltroHuerfanos;
            };

            ExpedientesView = collectionView;
            // Notificar cambios en los contadores
            OnPropertyChanged(nameof(TotalDocumentos));
            OnPropertyChanged(nameof(AutorizacionesCount));
            OnPropertyChanged(nameof(FacturasCount));
            OnPropertyChanged(nameof(InformesCount));
        }

        public ICommand ActualizarCommand
        {
            get
            {
                if (_actualizarCommand == null)
                {
                    _actualizarCommand = new RelayCommand(param => this.Actualizar(), null);
                }
                return _actualizarCommand;
            }
        }

        public ICommand AddSeleccionCommand
        {
            get
            {
                if (_addSeleccionCommand == null)
                {
                    _addSeleccionCommand = new RelayCommand<Expediente>(AgregarSeleccionado);
                }
                return _addSeleccionCommand;
            }
        }

        public ICommand RemoveSeleccionCommand
        {
            get
            {
                if (_removeSeleccionCommand == null)
                {
                    _removeSeleccionCommand = new RelayCommand<Expediente>(QuitarSeleccionado);
                }
                return _removeSeleccionCommand;
            }
        }

        private void Actualizar()
        {
            CargarExpedientes(AplicarFiltroFechas, FechaInicio, FechaFin);
        }

        private void AgregarSeleccionado(Expediente expediente)
        {
            if (expediente != null && !Seleccionados.Contains(expediente))
            {
                Seleccionados.Add(expediente);
            }
        }

        private void QuitarSeleccionado(Expediente expediente)
        {
            if (expediente != null && Seleccionados.Contains(expediente))
            {
                Seleccionados.Remove(expediente);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}