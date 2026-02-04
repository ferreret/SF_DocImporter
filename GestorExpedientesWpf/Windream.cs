using GestorExpedientesWpf.Models;
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

namespace GestorExpedientesWpf
{
    public class Windream
    {
        // Declaramos las variables de módulo para la funcionalidad de Windream
        WMSession? _wmSession;
        WMConnect? _wmConnect;
        WMMsgHandler? _wmMsgHandler;
        ServerBrowser? _serverBrowser;

        public ObservableCollection<Expediente> GetExpedientes(bool filtroFecha, DateTime fechaInicio, DateTime fechaFin)
        {
            var result = new ObservableCollection<Expediente>();

            if (!Login2Windream())
            {
                return result;
            }

            // Recuperamos del archivo ini la unidad de red de windream y el nombre del object type
            var pathIniFile = Path.Combine(GetExecutablePath(), "GestorExpedientesWpf.ini");
            IniFile iniFile = new IniFile(pathIniFile);

            string? unidadRed = iniFile.ReadValue("Windream", "UnidadRed");
            string? objectTypeName = iniFile.ReadValue("Windream", "ObjectType");

            // Creamos un objeto de la clase WMSearch
            WMSearch? wmSearch = _wmSession!.CreateWMSearch(WMEntity.WMEntityDocument);
            IWMSearch4 wmSearch4 = (IWMSearch4)wmSearch;

            // Aplicamos filtro de fechas si procede
            if (filtroFecha)
            {
                wmSearch4.AddSearchTerm("DMS Created", fechaInicio, WMSearchOperator.WMSearchOperatorGreaterEqual, WMSearchRelation.WMSearchRelationAnd, 0, 0);
                wmSearch4.AddSearchTerm("DMS Created", fechaFin, WMSearchOperator.WMSearchOperatorLesserEqual, WMSearchRelation.WMSearchRelationAnd, 0, 0);
            }

            // Definimos las columnas a recuperar (las indicadas por el usuario)
            object[] columnas = new object[]
            {
                "dwDocID",          // 0
                "##ObjectPath##",   // 1 (ruta calculada, equivalente a .aPath)
                "dwFlags",          // 2 (flags / estado del objeto)
                "NoAutorizacion",   // 3
                "DMS Created",      // 4
                "Cobertura",        // 5
                "NIFMutua",         // 6
                "NombrePaciente",   // 7
                "DNIPaciente",      // 8
                "FechaFactura",     // 9
                "NoFactura",        // 10
                "Remesa",           // 11
                "CoberturaInforme", // 12
                "TipoDoc",          // 13
                "FechaActo",        // 14
                "NoActo"            // 15
            };

            IWMObjects3? searchResult3 = null;

            try
            {
                // Ejecutamos la búsqueda en modo valores y sin contar (optimizado)
                WMSearchMode searchMode = (WMSearchMode) (WMSearchMode.WMSearchModeValues | WMSearchMode.WMSearchModeNoCount);
                searchResult3 = (IWMObjects3)wmSearch4.ExecuteEx(searchMode);

                int batchSize = 5000;
                bool moreData = true;

                while (moreData)
                {
                    object valuesObj = searchResult3.GetValues(batchSize, 0, columnas);
                    Array values = (Array)valuesObj;

                    // La matriz devuelta es [columna, fila]
                    int filas = values.GetLength(1);

                    for (int fila = 0; fila < filas; fila++)
                    {
                        try
                        {
                            // Extraemos las columnas por índice según el array 'columnas'
                            object? docIdRaw = values.GetValue(0, fila);
                            string objectPath = Convert.ToString(values.GetValue(1, fila)) ?? string.Empty;
                            int fsStatus = values.GetValue(2, fila) != null ? Convert.ToInt32(values.GetValue(2, fila)) : 0;
                            object? noAutorizacionRaw = values.GetValue(3, fila);
                            object? dmsCreatedRaw = values.GetValue(4, fila);
                            object? coberturaRaw = values.GetValue(5, fila);
                            object? nifMutuaRaw = values.GetValue(6, fila);
                            object? nombrePacienteRaw = values.GetValue(7, fila);
                            object? dniPacienteRaw = values.GetValue(8, fila);
                            object? fechaFacturaRaw = values.GetValue(9, fila);
                            object? noFacturaRaw = values.GetValue(10, fila);
                            object? remesaRaw = values.GetValue(11, fila);
                            object? coberturaInformeRaw = values.GetValue(12, fila);
                            object? tipoDocRaw = values.GetValue(13, fila);
                            object? fechaActoRaw = values.GetValue(14, fila);
                            object? noActoRaw = values.GetValue(15, fila);

                            if (TieneEstado(fsStatus, WMObjectStatus.WMObjectStatusPreVersion))
                            {
                                continue;
                            }

                            int docID = docIdRaw != null ? Convert.ToInt32(docIdRaw) : 0;
                            DateTime fechaCreacion = dmsCreatedRaw != null ? Convert.ToDateTime(dmsCreatedRaw) : DateTime.MinValue;
                            DateTime fechaFactura = fechaFacturaRaw != null ? Convert.ToDateTime(fechaFacturaRaw) : DateTime.MinValue;
                            DateTime fechaActo = fechaActoRaw != null ? Convert.ToDateTime(fechaActoRaw) : DateTime.MinValue;

                            Expediente expediente = new Expediente
                            {
                                DocID = docID,
                                RutaWindream = string.IsNullOrWhiteSpace(unidadRed)
                                                ? @"\\Windream\Objects\" + objectPath
                                                : unidadRed + ":" + objectPath,
                                NoAutorizacion = noAutorizacionRaw?.ToString() ?? string.Empty,
                                FechaCreacion = fechaCreacion,
                                Cobertura = coberturaRaw?.ToString() ?? string.Empty,
                                NIFMutua = nifMutuaRaw?.ToString() ?? string.Empty,
                                NombrePaciente = nombrePacienteRaw?.ToString() ?? string.Empty,
                                DNIPaciente = dniPacienteRaw?.ToString() ?? string.Empty,
                                FechaFactura = fechaFactura,
                                NoFactura = noFacturaRaw?.ToString() ?? string.Empty,
                                Remesa = remesaRaw?.ToString() ?? string.Empty,
                                CoberturaInforme = coberturaInformeRaw?.ToString() ?? string.Empty,
                                TipoDoc = tipoDocRaw?.ToString() ?? string.Empty,
                                FechaActo = fechaActo,
                                NoActo = noActoRaw?.ToString() ?? string.Empty,
                                IsOrphan = false
                            };

                            result.Add(expediente);
                        }
                        catch (Exception exRow)
                        {
                            // No interrumpimos la importación por una fila defectuosa
                            MessageBox.Show(exRow.Message);
                        }
                    }

                    // Si hemos recibido menos filas que las solicitadas, no hay más datos
                    if (filas < batchSize)
                    {
                        moreData = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error recuperando expedientes: {ex.Message}", "Gestor Expedientes", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (searchResult3 != null)
                {
                    try
                    {
                        ((IWMObjects4)searchResult3).Clear();
                    }
                    catch { }
                    try
                    {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(searchResult3);
                    }
                    catch { }
                }
            }

            return result;
        }

        public bool TieneEstado(int valor, WMObjectStatus estadoBuscado)
        {
            return (valor & (int)estadoBuscado) != 0;
        }

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

        public void AsignarMetadatosAExpedientes(Expediente editExpediente, ObservableCollection<Expediente> seleccionados)
        {
            // Implementación de la función que asigna metadatos
            if (!Login2Windream())
            {
                throw new InvalidOperationException("No se pudo conectar a Windream.");
            }

            // Hacemos un bucle por cada expediente seleccionado
            foreach (Expediente expediente in seleccionados)
            {
                try
                {
                    WMObject document = _wmSession!.GetWMObjectById(WMEntity.WMEntityDocument, expediente.DocID);

                    if (!PrepareDocumentForEditing(document))
                    {
                        MessageBox.Show("No se pudo bloquear el documento para edición.", "Gestor Expedientes", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }

                    document.SetVariableValue("NoAutorizacion", editExpediente.NoAutorizacion);
                    document.AddHistory($"NoAutorizacion: {expediente.NoAutorizacion} -> {editExpediente.NoAutorizacion}");
                    document.SetVariableValue("Cobertura", editExpediente.Cobertura);
                    document.AddHistory($"Cobertura: {expediente.Cobertura} -> {editExpediente.Cobertura}");
                    document.SetVariableValue("NIFMutua", editExpediente.NIFMutua);
                    document.AddHistory($"NIFMutua: {expediente.NIFMutua} -> {editExpediente.NIFMutua}");
                    document.SetVariableValue("NombrePaciente", editExpediente.NombrePaciente);
                    document.AddHistory($"NombrePaciente: {expediente.NombrePaciente} -> {editExpediente.NombrePaciente}");
                    document.SetVariableValue("DNIPaciente", editExpediente.DNIPaciente);
                    document.AddHistory($"DNIPaciente: {expediente.DNIPaciente} -> {editExpediente.DNIPaciente}");
                    document.SetVariableValue("FechaFactura", editExpediente.FechaFactura);
                    document.AddHistory($"FechaFactura: {expediente.FechaFactura} -> {editExpediente.FechaFactura}");
                    document.SetVariableValue("NoFactura", editExpediente.NoFactura);
                    document.AddHistory($"NoFactura: {expediente.NoFactura} -> {editExpediente.NoFactura}");
                    document.SetVariableValue("Remesa", editExpediente.Remesa);
                    document.AddHistory($"Remesa: {expediente.Remesa} -> {editExpediente.Remesa}");
                    document.SetVariableValue("CoberturaInforme", editExpediente.CoberturaInforme);
                    document.AddHistory($"CoberturaInforme: {expediente.CoberturaInforme} -> {editExpediente.CoberturaInforme}");
                    document.AddHistory($"FechaActo: {expediente.FechaActo} -> {editExpediente.FechaActo}");
                    document.SetVariableValue("FechaActo", editExpediente.FechaActo);
                    document.AddHistory($"NoActo: {expediente.NoActo} -> {editExpediente.NoActo}");
                    document.SetVariableValue("NoActo", editExpediente.NoActo);

                    document.Save();
                    document.unlock();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        public void EliminarExpedientes(ObservableCollection<Expediente> lista)
        {
            // Implementación de la función que asigna metadatos
            if (!Login2Windream())
            {
                throw new InvalidOperationException("No se pudo conectar a Windream.");
            }

            foreach (Expediente expediente in lista)
            {
                try
                {
                    
                    WMObject document = _wmSession!.GetWMObjectById(WMEntity.WMEntityDocument, expediente.DocID);
                                        
                    foreach (WMObject version in document.aVersions)
                    {
                        if (!PrepareDocumentForDelete(version))
                        {
                            MessageBox.Show($"No se pudo bloquear el documento {document.aName} para eliminar.", "Gestor Expedientes", MessageBoxButton.OK, MessageBoxImage.Error);
                            continue;
                        }
                        IWMObject6 doc = (IWMObject6)version;
                        doc.DeleteEx((int)WMDeleteFlags.WMDeleteFlags_Default);
                    }                                  
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private bool PrepareDocumentForDelete(WMObject document)
        {
            if (!document.IsEditableFor((int)WMObjectEditMode.WMObjectEditModeDelete))
            {
                return false;
            }

            if (!document.LockFor((int)WMObjectEditMode.WMObjectEditModeDelete))
            {
                return false;
            }

            return true;
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
    }
}
