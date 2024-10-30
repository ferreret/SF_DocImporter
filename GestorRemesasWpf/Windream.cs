using GestorRemesasWpf.Models;
using LibUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WINDREAMLib;
using WMCNNCTDLLLib;
using WMOBRWSLib;
using WMOMISCDLLLib;

namespace GestorRemesasWpf
{
    public class Windream
    {
        // Declaramos las variables de módulo para la funcionalidad de Windream
        WMSession? _wmSession;
        WMConnect? _wmConnect;
        WMMsgHandler? _wmMsgHandler;
        ServerBrowser? _serverBrowser;

        private bool Login2Windream()
        {
            try
            {
                _wmConnect = new WMConnect();
                _serverBrowser = new ServerBrowser();
                _wmMsgHandler = new WMMsgHandler();

                string? lstrServidor;
                lstrServidor = _serverBrowser.GetCurrentServer();

                if (lstrServidor != null && lstrServidor.Length > 0)
                {
                    Type? lobjSrvType = Type.GetTypeFromProgID("Windream.WMSession", lstrServidor, true);
                    _wmConnect.ServerName = lstrServidor;
                    if (lobjSrvType != null)
                    {
                        _wmSession = Activator.CreateInstance(lobjSrvType) as WMSession;
                    }
                    else
                    {
                        throw new InvalidOperationException("No se pudo obtener el tipo de servidor Windream.");
                    }

                    _wmConnect.LoginSession(_wmSession);

                    if (_wmSession!.aLoggedin)
                    {
                        return true;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                // mobjWMMsgHandler.ComErrHRes = ex.ErrorCode;
                // mobjWMMsgHandler.ComErrDesc = ex.Message;
                // mobjWMMsgHandler.ShowError("COM-Exception: " + ex.ErrorCode.ToString("x"));
                MessageBox.Show("Error en la conexión a windream: " + ex.Message, "Gestor Expedientes", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                // mobjWMMsgHandler.ComErrHRes = ex.GetHashCode();
                // mobjWMMsgHandler.ComErrDesc = ex.Message;
                // mobjWMMsgHandler.ShowError("COM-Exception: " + ex.GetHashCode().ToString("x"));
                MessageBox.Show("Error en la conexión de windream: " + ex.Message, "Gestor Expedientes", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return false;
        }

        // -------------------------------------------------------------------------------------------------------
        // FUNCTION: GetDocumentType
        // Función que obtiene el tipo de documento en Windream
        // NBL 2024/04/17
        // -------------------------------------------------------------------------------------------------------
        public WMObject? GetDocumentType(string typeName)
        {
            if (_wmSession == null)
            {
                throw new InvalidOperationException("La sesión de Windream no está inicializada.");
            }

            WMObjects oObjectTypes = _wmSession.GetAllObjects(WMEntity.WMEntityObjectType);

            foreach (WMObject oObjectType in oObjectTypes)
            {
                if (oObjectType.aName == typeName)
                {
                    return oObjectType;
                }
            }

            return null;
        }

        public string GetExecutablePath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public bool TieneEstado(int valor, WMObjectStatus estadoBuscado)
        {
            return (valor & (int)estadoBuscado) != 0;
        }

        public ObservableCollection<Expediente> GetExpedientes(bool filtroFecha, DateTime fechaInicio, DateTime fechaFin)
        {
            var result = new ObservableCollection<Expediente>();

            if (!Login2Windream())
            {
                return result;
            }

            // Recuperamos del archivo ini la unidad de red de windream y el nombre del object type
            var pathIniFile = Path.Combine(GetExecutablePath(), "GestorRemesasWpf.ini");
            IniFile iniFile = new IniFile(pathIniFile);

            string? unidadRed = iniFile.ReadValue("Windream", "UnidadRed");
            string? objectTypeName = iniFile.ReadValue("Windream", "ObjectType");

            // Obtenemos el tipo de documento en Windream
            WMObject? objectType = GetDocumentType(objectTypeName);

            if (objectType == null)
            {
                MessageBox.Show("No se encontró el tipo de documento en Windream.", "Gestor Expedientes", MessageBoxButton.OK, MessageBoxImage.Error);
                return result;
            }

            // Creamos un objeto de la clase WMSearch
            WMSearch? wmSearch = _wmSession!.CreateWMSearch(WMEntity.WMEntityDocument);


            if (filtroFecha)
            {
                wmSearch.AddSearchTerm("DMS Created", fechaInicio, WMSearchOperator.WMSearchOperatorGreaterEqual, WMSearchRelation.WMSearchRelationAnd, 0, 0);
                wmSearch.AddSearchTerm("DMS Created", fechaFin, WMSearchOperator.WMSearchOperatorLesserEqual, WMSearchRelation.WMSearchRelationAnd, 0, 0);
            }

            WMObjects lobjSearchResult = wmSearch.ExecuteEx(WMSearchMode.WMSearchModeNoCount | WMSearchMode.WMSearchModeValues);

            ArrayList lobjListaDocuments = new();
            string[] VariablesNames = { "szLongName", "dwDocID", "dwDocDBID" };
            int MAXFETCHCOUNT = 50;
            Array ResultList;

            try
            {
                ResultList = (Array)lobjSearchResult.GetValues(MAXFETCHCOUNT, 0, VariablesNames);
                // Hacemos un bucle por cada uno
                int upperBound = ResultList.GetUpperBound(1);
                for (int lintContador = 0; lintContador <= upperBound; lintContador++)
                {
                    lobjListaDocuments.Add(ResultList.GetValue(1, lintContador));
                }
            }
            catch (Exception)
            {
                // Si hay un error es que no hay datos para el número de petición dado
                MessageBox.Show("No se encontraron documentos en Windream.", "Gestor Expedientes",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return result;
            }

            foreach (var documento in lobjListaDocuments)
            {
                try
                {
                    WMObject document = _wmSession!.GetWMObjectById(WMEntity.WMEntityDocument, (int)documento!);

                    if (TieneEstado(document.aWMObjectStatus, WMObjectStatus.WMObjectStatusPreVersion))
                    {
                        continue;
                    }

                    Expediente expediente = new Expediente
                    {
                        DocID = (int)documento!,
                        RutaWindream = unidadRed + ":" + document.aPath,
                        NoAutorizacion = document.GetVariableValue("NoAutorizacion")?.ToString() ?? string.Empty,
                        FechaCreacion = document.GetVariableValue("DMS Created") != null ? (DateTime)document.GetVariableValue("DMS Created") : DateTime.MinValue,
                        Cobertura = document.GetVariableValue("Cobertura")?.ToString() ?? string.Empty,
                        NIFMutua = document.GetVariableValue("NIFMutua")?.ToString() ?? string.Empty,
                        NombrePaciente = document.GetVariableValue("NombrePaciente")?.ToString() ?? string.Empty,
                        DNIPaciente = document.GetVariableValue("DNIPaciente")?.ToString() ?? string.Empty,
                        FechaFactura = document.GetVariableValue("FechaFactura") != null ? (DateTime)document.GetVariableValue("FechaFactura") : DateTime.MinValue,
                        NoFactura = document.GetVariableValue("NoFactura")?.ToString() ?? string.Empty,
                        Remesa = document.GetVariableValue("Remesa")?.ToString() ?? string.Empty,
                        CoberturaInforme = document.GetVariableValue("CoberturaInforme")?.ToString() ?? string.Empty,
                        TipoDoc = document.GetVariableValue("TipoDoc")?.ToString() ?? string.Empty,
                        IsOrphan = false
                    };

                    result.Add(expediente);
                }

                catch (Exception ex)
                {
                    // No hacemos nada
                    MessageBox.Show(ex.Message);
                }
            }

            return result;
        }

        private bool PrepareDocumentForEditing(WMObject document)
        {
            if (!document.IsEditableFor((int)WMObjectEditMode.WMObjectEditModeObjectAndRights))
            {
                return false;
            }


            if (!document.LockFor((int)WMObjectEditMode.WMObjectEditModeObjectAndRights))
            {
                return false;
            }

            return true;
        }

        public void AsignarRemesaExpediente(List<int> docIds, string remesa)
        {
            if (!Login2Windream())
            {
                throw new InvalidOperationException("No se pudo conectar a Windream.");
            }

            try
            {
                foreach (var docId in docIds)
                {
                    WMObject document = _wmSession!.GetWMObjectById(WMEntity.WMEntityDocument, docId);

                    if (!PrepareDocumentForEditing(document))
                    {
                        MessageBox.Show("No se pudo bloquear el documento para edición.", "Gestor Expedientes", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }

                    if (document == null)
                    {
                        throw new InvalidOperationException("No se encontró el documento en Windream.");
                    }

                    document.SetVariableValue("Remesa", remesa);
                    document.AddHistory("Remesa asignada: " + remesa);
                    document.Save();
                    document.unlock();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al asignar la remesa al expediente: " + ex.Message, "Gestor Expedientes", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
