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
        #region Campos Privados

        private readonly Windream _windream;

        private ObservableCollection<Expediente>? _expedientes;
        private ObservableCollection<Expediente>? _seleccionados;

        private bool _aplicarFiltroFechas;
        private DateTime _fechaInicio;
        private DateTime _fechaFin;

        private ICommand _actualizarCommand;
        private ICommand _addSeleccionCommand;
        private ICommand _removeSeleccionCommand;
        private ICommand _asignarMetadatosCommand;
        private ICommand _showEditMetadataCommand;
        private ICommand _cancelEditMetadataCommand;
        private ICommand _setMetadataCommand;    

        private bool _ignorarDocumentosConRemesa;
        private bool _mostrarSoloDocumentosHuerfanos;

        private ICollectionView _expedientesView;

        private Expediente? _selectedExpediente;
        private Expediente? _editExpediente;
        private bool _isBusy;
        private bool _asegurarTriplete;
        private bool _editMetadataMode;

        #endregion

        #region Propiedades Públicas

        public string SelectedExpedienteUrl => SelectedExpediente?.RutaWindream ?? "about:blank";

        public bool AsegurarTriplete
        {
            get => _asegurarTriplete;
            set
            {
                _asegurarTriplete = value;
                OnPropertyChanged(nameof(AsegurarTriplete));
            }
        }

        public bool EditMetadataMode
        {
            get => _editMetadataMode;
            set
            {               
               _editMetadataMode = value;
               OnPropertyChanged(nameof(EditMetadataMode));
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

        public Expediente? EditExpediente
        {
            get => _editExpediente;
            set
            {
                _editExpediente = value;
                OnPropertyChanged(nameof(EditExpediente));
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

        #endregion

        #region Comandos

        public ICommand ActualizarCommand => _actualizarCommand ??= new RelayCommand(param => Actualizar(), null);
        public ICommand AddSeleccionCommand => _addSeleccionCommand ??= new RelayCommand<Expediente>(AgregarSeleccionado);
        public ICommand RemoveSeleccionCommand => _removeSeleccionCommand ??= new RelayCommand<Expediente>(QuitarSeleccionado);
        public ICommand AsignarMetadatosCommand => _asignarMetadatosCommand ??= new RelayCommand(param => AsignarMetadatos(), null);
        public ICommand ShowEditMetadataCommand => _showEditMetadataCommand ??= new RelayCommand(param => EditMetadataMode = true, null);
        public ICommand CancelEditMetadataCommand => _cancelEditMetadataCommand ??= new RelayCommand(param => EditMetadataMode = false, null);
        public ICommand SetMetadataCommand => _setMetadataCommand ??= new RelayCommand(param => SetMetadata(), null);

        #endregion

        #region Constructor

        public ExpedientesViewModel()
        {
            _windream = new Windream();

            FechaInicio = DateTime.Now;
            FechaFin = DateTime.Now;

            IgnorarDocumentosConRemesa = true;
            MostrarSoloDocumentosHuerfanos = false;
            AsegurarTriplete = true;
            EditMetadataMode = false;

        }

        #endregion

        #region Métodos Públicos

        public void CargarExpedientes(bool aplicarFiltroFechas, DateTime fechaInicio, DateTime fechaFin)
        {
            var expedientes = _windream.GetExpedientes(aplicarFiltroFechas, fechaInicio, fechaFin);
            Expedientes = expedientes;
            CalcularIsOrphan();
            FiltrarExpedientes();
        }

        #endregion

        #region Métodos Privados

        private void SetMetadata()
        {
            try
            {
                _windream.AsignarMetadatosAExpedientes(EditExpediente!, Seleccionados);
                EditMetadataMode = false;
                // Reseteamos la selección
                Seleccionados.Clear();
                // Actualizamos la lista de expedientes
                Actualizar();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al asignar metadatos: " + ex.Message, "Gestor Expedientes", MessageBoxButton.OK, MessageBoxImage.Error);
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
            OnPropertyChanged(nameof(TotalDocumentos));
            OnPropertyChanged(nameof(AutorizacionesCount));
            OnPropertyChanged(nameof(FacturasCount));
            OnPropertyChanged(nameof(InformesCount));
        }

        private void AsignarMetadatos()
        {
            if (AsegurarTriplete)
            {
                bool tieneAutorizacion = Seleccionados.Any(e => e.TipoDoc == "Autorización");
                bool tieneFactura = Seleccionados.Any(e => e.TipoDoc == "Factura");
                bool tieneInforme = Seleccionados.Any(e => e.TipoDoc == "Informe");

                if (!tieneAutorizacion || !tieneFactura || !tieneInforme)
                {
                    MessageBox.Show("Debe haber al menos una autorización, una factura y un informe en la selección.", "Gestión Expedientes",
                         MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }
            else if (!Seleccionados.Any())
            {
                MessageBox.Show("Debe haber al menos un registro en la selección.", "Gestión Expedientes",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            // Tenemos que poner en los controles del modo de edición los valores que corresponda
            // Crear EditExpediente incluso si no hay una factura en la selección
            EditExpediente = new Expediente();

            // Asignar EditExpediente basado en la factura seleccionada si existe
            var factura = Seleccionados.FirstOrDefault(e => e.TipoDoc == "Factura");
            if (factura != null)
            {
                EditExpediente.DocID = factura.DocID;
                EditExpediente.RutaWindream = factura.RutaWindream;
                EditExpediente.NoAutorizacion = factura.NoAutorizacion;
                EditExpediente.FechaCreacion = factura.FechaCreacion;
                EditExpediente.Cobertura = factura.Cobertura;
                EditExpediente.NIFMutua = factura.NIFMutua;
                EditExpediente.NombrePaciente = factura.NombrePaciente;
                EditExpediente.DNIPaciente = factura.DNIPaciente;
                EditExpediente.FechaFactura = factura.FechaFactura;
                EditExpediente.NoFactura = factura.NoFactura;
                EditExpediente.Remesa = factura.Remesa;
                EditExpediente.CoberturaInforme = factura.CoberturaInforme;
                EditExpediente.TipoDoc = factura.TipoDoc;
                EditExpediente.IsOrphan = factura.IsOrphan;
            }

            // Rellenar campos vacíos con datos del primer expediente seleccionado que tenga algún valor para ese campo específico
            foreach (var expediente in Seleccionados)
            {
                if (string.IsNullOrEmpty(EditExpediente.RutaWindream))
                    EditExpediente.RutaWindream = expediente.RutaWindream;
                if (string.IsNullOrEmpty(EditExpediente.NoAutorizacion))
                    EditExpediente.NoAutorizacion = expediente.NoAutorizacion;
                if (string.IsNullOrEmpty(EditExpediente.Cobertura))
                    EditExpediente.Cobertura = expediente.Cobertura;
                if (string.IsNullOrEmpty(EditExpediente.NIFMutua))
                    EditExpediente.NIFMutua = expediente.NIFMutua;
                if (string.IsNullOrEmpty(EditExpediente.NombrePaciente))
                    EditExpediente.NombrePaciente = expediente.NombrePaciente;
                if (string.IsNullOrEmpty(EditExpediente.DNIPaciente))
                    EditExpediente.DNIPaciente = expediente.DNIPaciente;
                if (string.IsNullOrEmpty(EditExpediente.NoFactura))
                    EditExpediente.NoFactura = expediente.NoFactura;
                if (string.IsNullOrEmpty(EditExpediente.Remesa))
                    EditExpediente.Remesa = expediente.Remesa;
                if (string.IsNullOrEmpty(EditExpediente.CoberturaInforme))
                    EditExpediente.CoberturaInforme = expediente.CoberturaInforme;
            }

            // Entramos en modo edición de metadatos
            EditMetadataMode = true;            
        }

        private async void Actualizar()
        {
            IsBusy = true;

            try
            {
                await Task.Run(() => CargarExpedientes(AplicarFiltroFechas, FechaInicio, FechaFin));
            }
            finally
            {
                IsBusy = false;
            }
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

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}