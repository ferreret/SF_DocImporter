using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestorRemesasWpf.Models
{

    public enum FacturaState
    {
        NotInList, // No está en la lista de facturas que se han cargado en el archivo de texto
        Ready, // En la lista, no es huerfano y no tiene código de Remesa
        MissingDocuments, // En la lista y es huerfano
        RemesaMissing // En la lista, no es huerfano pero tiene código de Remesa
    }

    public class Expediente : INotifyPropertyChanged
    {
        private int _docID;
        private string? _rutaWindream;
        private string? _noAutorizacion;
        private DateTime _fechaCreacion;
        private string? _cobertura;
        private string? _nifMutua;
        private string? _nombrePaciente;
        private string? _dniPaciente;
        private DateTime _fechaFactura;
        private string? _noFactura;
        private string? _remesa;
        private string? _coberturaInforme;
        private string? _tipoDoc;
        private bool _isOrphan;
        private FacturaState _facturaState;
        private bool _esFacturaCargada;
        private bool _esFacturaOrphan;
        private bool _esFacturaInvalida;
        private bool _faltaInforme;
        private bool _faltaAutorizacion;
        private decimal _importeFactura;

        public decimal ImporteFactura
        {
            get => _importeFactura;
            set
            {
                if (_importeFactura != value)
                {
                    _importeFactura = value;
                    OnPropertyChanged(nameof(ImporteFactura));
                }
            }
        }

        public int DocID
        {
            get => _docID;
            set
            {
                _docID = value;
                OnPropertyChanged(nameof(DocID));
            }
        }
        public string? RutaWindream
        {
            get => _rutaWindream ?? string.Empty;
            set
            {
                _rutaWindream = value;
                OnPropertyChanged(nameof(RutaWindream));
            }
        }
        public string NoAutorizacion
        {
            get => _noAutorizacion ?? string.Empty;
            set
            {
                _noAutorizacion = value;
                OnPropertyChanged(nameof(NoAutorizacion));
            }
        }
        public DateTime FechaCreacion
        {
            get => _fechaCreacion;
            set
            {
                _fechaCreacion = value;
                OnPropertyChanged(nameof(FechaCreacion));
            }
        }
        public string Cobertura
        {
            get => _cobertura ?? string.Empty;
            set
            {
                _cobertura = value;
                OnPropertyChanged(nameof(Cobertura));
            }
        }
        public string NIFMutua
        {
            get => _nifMutua ?? string.Empty;
            set
            {
                _nifMutua = value;
                OnPropertyChanged(nameof(NIFMutua));
            }
        }
        public string NombrePaciente
        {
            get => _nombrePaciente ?? string.Empty;
            set
            {
                _nombrePaciente = value;
                OnPropertyChanged(nameof(NombrePaciente));
            }
        }
        public string DNIPaciente
        {
            get => _dniPaciente ?? string.Empty;
            set
            {
                _dniPaciente = value;
                OnPropertyChanged(nameof(DNIPaciente));
            }
        }
        public DateTime FechaFactura
        {
            get => _fechaFactura;
            set
            {
                _fechaFactura = value;
                OnPropertyChanged(nameof(FechaFactura));
            }
        }
        public string NoFactura
        {
            get => _noFactura ?? string.Empty;
            set
            {
                _noFactura = value;
                OnPropertyChanged(nameof(NoFactura));
            }
        }
        public string Remesa
        {
            get => _remesa ?? string.Empty;
            set
            {
                _remesa = value;
                OnPropertyChanged(nameof(Remesa));
            }
        }
        public string CoberturaInforme
        {
            get => _coberturaInforme ?? string.Empty;
            set
            {
                _coberturaInforme = value;
                OnPropertyChanged(nameof(CoberturaInforme));
            }
        }
        public string TipoDoc
        {
            get => _tipoDoc ?? string.Empty;
            set
            {
                _tipoDoc = value;
                OnPropertyChanged(nameof(TipoDoc));
            }
        }
        public bool IsOrphan
        {
            get => _isOrphan;
            set
            {
                _isOrphan = value;
                OnPropertyChanged(nameof(IsOrphan));
            }
        }

        public bool EsFacturaCargada
        {
            get => _esFacturaCargada;
            set
            {
                if (_esFacturaCargada != value)
                {
                    _esFacturaCargada = value;
                    OnPropertyChanged(nameof(EsFacturaCargada));
                }
            }
        }

        public bool EsFacturaOrphan
        {
            get => _esFacturaOrphan;
            set
            {
                if (_esFacturaOrphan != value)
                {
                    _esFacturaOrphan = value;
                    OnPropertyChanged(nameof(EsFacturaOrphan));
                }
            }
        }

        public bool EsFacturaInvalida
        {
            get => _esFacturaInvalida;
            set
            {
                if (_esFacturaInvalida != value)
                {
                    _esFacturaInvalida = value;
                    OnPropertyChanged(nameof(EsFacturaInvalida));
                }
            }
        }

        public bool FaltaInforme
        {
            get => _faltaInforme;
            set
            {
                if (_faltaInforme != value)
                {
                    _faltaInforme = value;
                    OnPropertyChanged(nameof(FaltaInforme));
                }
            }
        }

        public bool FaltaAutorizacion
        {
            get => _faltaAutorizacion;
            set
            {
                if (_faltaAutorizacion != value)
                {
                    _faltaAutorizacion = value;
                    OnPropertyChanged(nameof(FaltaAutorizacion));
                }
            }
        }

        public bool EsFacturaSinAutorizacion
        {
            get => EsFacturaCargada && FaltaAutorizacion;
        }

        public bool EsFacturaSinInforme
        {
            get => EsFacturaCargada && FaltaInforme;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
