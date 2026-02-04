using GestorRemesasWpf.Models;
using LibUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices;
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

        //public ObservableCollection<Expediente> GetExpedientes(bool filtroFecha, DateTime fechaInicio, DateTime fechaFin)
        //{
        //    var result = new ObservableCollection<Expediente>();

        //    if (!Login2Windream())
        //    {
        //        return result;
        //    }

        //    // Recuperamos del archivo ini la unidad de red de windream y el nombre del object type
        //    var pathIniFile = Path.Combine(GetExecutablePath(), "GestorRemesasWpf.ini");
        //    IniFile iniFile = new IniFile(pathIniFile);

        //    string? unidadRed = iniFile.ReadValue("Windream", "UnidadRed");
        //    string? objectTypeName = iniFile.ReadValue("Windream", "ObjectType");

        //    // Obtenemos el tipo de documento en Windream
        //    WMObject? objectType = GetDocumentType(objectTypeName);

        //    if (objectType == null)
        //    {
        //        MessageBox.Show("No se encontró el tipo de documento en Windream.", "Gestor Expedientes", MessageBoxButton.OK, MessageBoxImage.Error);
        //        return result;
        //    }

        //    // Creamos un objeto de la clase WMSearch
        //    WMSearch? wmSearch = _wmSession!.CreateWMSearch(WMEntity.WMEntityDocument);


        //    if (filtroFecha)
        //    {
        //        wmSearch.AddSearchTerm("DMS Created", fechaInicio, WMSearchOperator.WMSearchOperatorGreaterEqual, WMSearchRelation.WMSearchRelationAnd, 0, 0);
        //        wmSearch.AddSearchTerm("DMS Created", fechaFin, WMSearchOperator.WMSearchOperatorLesserEqual, WMSearchRelation.WMSearchRelationAnd, 0, 0);
        //    }

        //    WMObjects lobjSearchResult = wmSearch.ExecuteEx(WMSearchMode.WMSearchModeNoCount | WMSearchMode.WMSearchModeValues);

        //    ArrayList lobjListaDocuments = new();
        //    string[] VariablesNames = { "szLongName", "dwDocID", "dwDocDBID" };
        //    int MAXFETCHCOUNT = 100000;
        //    Array ResultList;

        //    try
        //    {
        //        ResultList = (Array)lobjSearchResult.GetValues(MAXFETCHCOUNT, 0, VariablesNames);
        //        // Hacemos un bucle por cada uno
        //        int upperBound = ResultList.GetUpperBound(1);
        //        for (int lintContador = 0; lintContador <= upperBound; lintContador++)
        //        {
        //            lobjListaDocuments.Add(ResultList.GetValue(1, lintContador));
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        // Si hay un error es que no hay datos para el número de petición dado
        //        MessageBox.Show("No se encontraron documentos en Windream.", "Gestor Expedientes",
        //            MessageBoxButton.OK, MessageBoxImage.Information);
        //        return result;
        //    }

        //    foreach (var documento in lobjListaDocuments)
        //    {
        //        try
        //        {
        //            WMObject document = _wmSession!.GetWMObjectById(WMEntity.WMEntityDocument, (int)documento!);

        //            if (TieneEstado(document.aWMObjectStatus, WMObjectStatus.WMObjectStatusPreVersion))
        //            {
        //                continue;
        //            }

        //            Expediente expediente = new Expediente
        //            {
        //                DocID = (int)documento!,
        //                RutaWindream = unidadRed.Trim().Length == 0 ? @"\\Windream\Objects\" + document.aPath : unidadRed + ":" + document.aPath,
        //                NoAutorizacion = document.GetVariableValue("NoAutorizacion")?.ToString() ?? string.Empty,
        //                FechaCreacion = document.GetVariableValue("DMS Created") != null ? (DateTime)document.GetVariableValue("DMS Created") : DateTime.MinValue,
        //                Cobertura = document.GetVariableValue("Cobertura")?.ToString() ?? string.Empty,
        //                NIFMutua = document.GetVariableValue("NIFMutua")?.ToString() ?? string.Empty,
        //                NombrePaciente = document.GetVariableValue("NombrePaciente")?.ToString() ?? string.Empty,
        //                DNIPaciente = document.GetVariableValue("DNIPaciente")?.ToString() ?? string.Empty,
        //                FechaFactura = document.GetVariableValue("FechaFactura") != null ? (DateTime)document.GetVariableValue("FechaFactura") : DateTime.MinValue,
        //                NoFactura = document.GetVariableValue("NoFactura")?.ToString() ?? string.Empty,
        //                Remesa = document.GetVariableValue("Remesa")?.ToString() ?? string.Empty,
        //                CoberturaInforme = document.GetVariableValue("CoberturaInforme")?.ToString() ?? string.Empty,                        
        //                TipoDoc = document.GetVariableValue("TipoDoc")?.ToString() ?? string.Empty,
        //                FechaActo = document.GetVariableValue("FechaActo") != null ? (DateTime)document.GetVariableValue("FechaActo") : DateTime.MinValue,
        //                NoActo = document.GetVariableValue("NoActo")?.ToString() ?? string.Empty,
        //                IsOrphan = false
        //            };

        //            result.Add(expediente);
        //        }

        //        catch (Exception ex)
        //        {
        //            // No hacemos nada
        //            MessageBox.Show(ex.Message);
        //        }
        //    }

        //    return result;
        //}

        public ObservableCollection<Expediente> GetExpedientes(bool filtroFecha, DateTime fechaInicio, DateTime fechaFin)
        {
            var result = new ObservableCollection<Expediente>();

            if (!Login2Windream()) return result;

            var pathIniFile = Path.Combine(GetExecutablePath(), "GestorRemesasWpf.ini");
            IniFile iniFile = new IniFile(pathIniFile);
            string? unidadRed = iniFile.ReadValue("Windream", "UnidadRed");
            string? objectTypeName = iniFile.ReadValue("Windream", "ObjectType");

            // Configuración de búsqueda
            WMSearch wmSearch = _wmSession!.CreateWMSearch(WMEntity.WMEntityDocument);

            IWMSearch4 wmSearch4 = (IWMSearch4)wmSearch;

            // Nota: Es mejor filtrar por ObjectType en la búsqueda que buscar el objeto primero, 
            // pero mantenemos tu lógica original de buscar el tipo si es necesario para validación.

            if (filtroFecha)
            {
                wmSearch4.AddSearchTerm("DMS Created", fechaInicio, WMSearchOperator.WMSearchOperatorGreaterEqual, WMSearchRelation.WMSearchRelationAnd, 0, 0);
                wmSearch4.AddSearchTerm("DMS Created", fechaFin, WMSearchOperator.WMSearchOperatorLesserEqual, WMSearchRelation.WMSearchRelationAnd, 0, 0);
            }

            // Filtramos también por el tipo de objeto para optimizar
            //if (!string.IsNullOrEmpty(objectTypeName))
            //{
            //    wmSearch4.AddSearchTerm("Object Type", objectTypeName, WMSearchOperator.WMSearchOperatorEqual, WMSearchRelation.WMSearchRelationAnd, 0, 0);
            //}

            // 1. DEFINIR COLUMNAS A RECUPERAR (Mapeo directo a tus propiedades)
            // Asegúrate de que "szPath" y "fsStatus" son los nombres internos correctos en tu versión de Windream.
            object[] columnas = new object[] {
                "dwDocID",          // 0
                "##ObjectPath##",           // 1 (Equivalente a .aPath)
                "dwFlags",         // 2 (Equivalente a .aWMObjectStatus)
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

            try
            {
                // 2. EJECUTAR BÚSQUEDA OPTIMIZADA
                // WMSearchModeValuesOnly (8) + WMSearchModeNoCount (32) = 40
                WMSearchMode searchMode = (WMSearchMode)40;
                //WMObjects lobjSearchResult;
                IWMObjects3 searchResult3 = (IWMObjects3)wmSearch4.ExecuteEx(searchMode);

                int batchSize = 5000;
                bool moreData = true;

                // Bucle para recuperar todo sin saber el total (.Count)
                while (moreData)
                {
                    object resultMatrixObj = searchResult3.GetValues(batchSize, 0, columnas);

                    Array matrizValores = (Array)resultMatrixObj;

                    // En Windream la matriz es [Columna, Fila]
                    // GetLength(1) nos da el número de filas recuperadas en este bloque
                    int filasRecuperadas = matrizValores.GetLength(1);

                    // PROCESAR LOS DATOS AQUÍ
                    for (int i = 0; i < filasRecuperadas; i++)
                    {
                        // Ejemplo: Acceder a la primera columna (índice 0) de la fila i
                        // Recordatorio: matrizValores es [Columna, Fila]
                        // 'i' es el índice de la fila actual en el bucle for

                        // --- 0. ID del Documento (dwDocID) ---
                        // Generalmente es un entero (Int32 o Int64 dependiendo de la versión/configuración)
                        var docID = matrizValores.GetValue(0, i);

                        // --- 1. Ruta Completa (##ObjectPath##) ---
                        // Devuelve un String. Windream calcula esto dinámicamente.
                        string objectPath = Convert.ToString(matrizValores.GetValue(1, i));

                        // --- 2. Estado del Objeto (fsStatus) ---
                        // Es un mapa de bits (Long/Int32). Contiene flags como 'Locked', 'CheckedOut', etc.
                        int fsStatus = Convert.ToInt32(matrizValores.GetValue(2, i));

                        // --- 3. Número de Autorización ---
                        var noAutorizacion = matrizValores.GetValue(3, i);

                        // --- 4. Fecha de Creación (DMS Created) ---
                        // Devuelve un DateTime (o null si está vacío)
                        var dmsCreatedRaw = matrizValores.GetValue(4, i);
                        DateTime? dmsCreated = dmsCreatedRaw != null ? (DateTime?)Convert.ToDateTime(dmsCreatedRaw) : null;

                        // --- 5. Cobertura ---
                        var cobertura = Convert.ToString(matrizValores.GetValue(5, i));

                        // --- 6. NIF Mutua ---
                        var nifMutua = Convert.ToString(matrizValores.GetValue(6, i));

                        // --- 7. Nombre Paciente ---
                        var nombrePaciente = Convert.ToString(matrizValores.GetValue(7, i));

                        // --- 8. DNI Paciente ---
                        var dniPaciente = Convert.ToString(matrizValores.GetValue(8, i));

                        // --- 9. Fecha Factura ---
                        var fechaFacturaRaw = matrizValores.GetValue(9, i);
                        // Validación simple por si viene nulo
                        DateTime? fechaFactura = fechaFacturaRaw != null ? (DateTime?)Convert.ToDateTime(fechaFacturaRaw) : null;

                        // --- 10. Número Factura ---
                        var noFactura = Convert.ToString(matrizValores.GetValue(10, i));

                        // --- 11. Remesa ---
                        var remesa = matrizValores.GetValue(11, i);

                        // --- 12. Cobertura Informe ---
                        var coberturaInforme = matrizValores.GetValue(12, i);

                        // --- 13. Tipo Documento (TipoDoc) ---
                        var tipoDoc = Convert.ToString(matrizValores.GetValue(13, i));

                        // --- 14. Fecha Acto ---
                        var fechaActoRaw = matrizValores.GetValue(14, i);
                        DateTime? fechaActo = fechaActoRaw != null ? (DateTime?)Convert.ToDateTime(fechaActoRaw) : null;

                        // --- 15. Número de Acto ---
                        var noActo = matrizValores.GetValue(15, i);

                        if (TieneEstado(fsStatus, WMObjectStatus.WMObjectStatusPreVersion))
                            continue;

                        Expediente expediente = new Expediente
                        {
                            DocID = docID != null ? Convert.ToInt32(docID) : 0,
                            RutaWindream = string.IsNullOrWhiteSpace(unidadRed)
                                           ? @"\\Windream\Objects\" + objectPath
                                           : unidadRed + ":" + objectPath,
                            NoAutorizacion = noAutorizacion?.ToString() ?? string.Empty,
                            FechaCreacion = dmsCreated ?? DateTime.MinValue,
                            Cobertura = cobertura ?? string.Empty,
                            NIFMutua = nifMutua ?? string.Empty,
                            NombrePaciente = nombrePaciente ?? string.Empty,
                            DNIPaciente = dniPaciente ?? string.Empty,
                            FechaFactura = fechaFactura ?? DateTime.MinValue,
                            NoFactura = noFactura ?? string.Empty,
                            Remesa = remesa?.ToString() ?? string.Empty,
                            CoberturaInforme = coberturaInforme?.ToString() ?? string.Empty,
                            TipoDoc = tipoDoc ?? string.Empty,
                            FechaActo = fechaActo ?? DateTime.MinValue,
                            NoActo = noActo?.ToString() ?? string.Empty,
                            IsOrphan = false
                        };

                        result.Add(expediente);
                    }

                    // Si recuperamos menos filas que las solicitadas, hemos llegado al final
                    if (filasRecuperadas < batchSize)
                    {
                        moreData = false;
                    }
                }

                if (searchResult3 != null)
                {
                    ((IWMObjects4)searchResult3).Clear(); // [5]
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(searchResult3);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error recuperando expedientes: {ex.Message}", "Gestor Expedientes", MessageBoxButton.OK, MessageBoxImage.Error);
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
