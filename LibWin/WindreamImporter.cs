using LibUtil;
using LibUtil.Models;
using System.Collections;
using WINDREAMLib;
using WMCNNCTDLLLib;
using WMOBRWSLib;
using WMOMISCDLLLib;
using WMOTOOLLib;

namespace LibWin
{
    public enum OperadorBusqueda
    {
        Igual,
        Diferente
    }

    public sealed class WindreamImporter
    {
        // Declaramos las variables de módulo para la funcionalidad de Windream
        WMSession? _wmSession;
        WMConnect? _wmConnect;
        WMMsgHandler? _wmMsgHandler;
        ServerBrowser? _serverBrowser;

        // Utilidades
        public required ServiceConfig _serviceConfig;
        public required FileLogger _fileLogger;

        /// <summary>
        /// Importa un archivo PDF a Windream y realiza validaciones dependiendo del tipo de documento.
        /// </summary>
        /// <param name="pathPdf">La ruta del archivo PDF a importar.</param>
        /// <param name="windreamIndexes">Los índices relacionados con el documento en Windream.</param>
        /// <param name="fileLogger">El objeto FileLogger utilizado para registrar mensajes de log.</param>
        /// <param name="serviceConfig">La configuración del servicio.</param>
        /// <returns>True si la importación fue exitosa, False en caso contrario.</returns>
        public string Import(string pathPdf, WindreamIndexes windreamIndexes, FileLogger fileLogger, ServiceConfig serviceConfig)
        {

            _fileLogger = fileLogger;
            _serviceConfig = serviceConfig;

            // Importar el PDF a Windream
            if (!Login2Windream())
            {
                _fileLogger.LogError("Error al conectar con Windream");
                return "";
            }

            // Recuperamos el object type 
            if (string.IsNullOrEmpty(_serviceConfig.ObjectType))
            {
                _fileLogger.LogError("El tipo de objeto no está configurado.");
                return "";
            }

            WMObject? oObjectType = GetDocumentType(_serviceConfig.ObjectType);
            if (oObjectType == null)
            {
                _fileLogger.LogError("No se ha encontrado el tipo de documento en Windream");
                return "";
            }

            // Dependiendo del tipo de documento, la operativa será diferente
            if (windreamIndexes.TipoDoc == TipoDocumento.Autorización)
            {
                // Si no tenemos el No de Autorización, no podemos trabajar con el documento
                if (String.IsNullOrEmpty(windreamIndexes.NoAutorizacion))
                {
                    _fileLogger.LogError("No se ha especificado el número de autorización.");
                    return "";
                }
                return importNoFactura(pathPdf, windreamIndexes, oObjectType);
            }
            else if (windreamIndexes.TipoDoc == TipoDocumento.Factura)
            {
                // Si no tenemos el número de factura, no podemos trabajar con el documento
                if (String.IsNullOrEmpty(windreamIndexes.NoFactura) || !ValidateMutua(windreamIndexes.Cobertura, _serviceConfig.PathMutuas!))
                {
                    _fileLogger.LogError("Faltan datos claves en la Factura.");
                    return "";
                }
                return importFactura(pathPdf, windreamIndexes, oObjectType);
            }
            else if (windreamIndexes.TipoDoc == TipoDocumento.Informe)
            {
                // Para el informe, no hay ningún filtro ya que el número de autorización puede existir o no, pero no es necesario ya que 
                // en el momento de guardar el documento se puede NO disponer
                return importNoFactura(pathPdf, windreamIndexes, oObjectType, true);
            }
            else
            {
                _fileLogger.LogError("Tipo de documento no soportado.");
                return "";
            }
        }

        // -------------------------------------------------------------------------------------------------------
        private bool ValidateMutua(string? cobertura, string pathMutuas)
        {
            if (string.IsNullOrEmpty(cobertura))
            {
                return false;
            }

            var result = LibUtil.Common.FindUniqueMinLevenshtein(pathMutuas, cobertura);

            if (result.Item1 != null)
            {
                Console.WriteLine($"The unique value with the minimum Levenshtein distance is: {result.Item1} with a distance of {result.Item2}");
                _fileLogger.LogInformation($"The unique value with the minimum Levenshtein distance is: {result.Item1} with a distance of {result.Item2}");
                return _serviceConfig.MaxLevensthein >= result.Item2;
            }
            else
            {
                Console.WriteLine("The minimum Levenshtein distance is not unique.");
                _fileLogger.LogError("The minimum Levenshtein distance is not unique.");
                return false;
            }
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


        // -------------------------------------------------------------------------------------------------------
        private string importFactura(string pathPdf, WindreamIndexes windreamIndexes, WMObject objectType)
        {
            var searchTerms = BuildSearchTermsFactura(windreamIndexes);
            var listaDocumentos = BuscarDocumentos(objectType, searchTerms);

            WMObject document;
            IWMSession2? wmSession2 = _wmSession as IWMSession2;

            if (wmSession2 == null)
            {
                _fileLogger.LogError("No se pudo obtener la sesión de Windream.");
                return "";
            }

            // Desactivar el evento de indexación
            wmSession2.SwitchEvents((int)WMCOMEvent.WMCOMEventWMSessionNeedIndex, false);

            bool isNewDocument = false;

            if (IsNewDocument(listaDocumentos))
            {
                document = CreateNewDocument(wmSession2, windreamIndexes);
                isNewDocument = true;
            }
            else
            {
                document = CreateNewVersion(listaDocumentos);
                isNewDocument = false;
            }

            if (!PrepareDocumentForEditing(document))
            {
                return "";
            }

            bool uploadFactura = UploadPdfToWindream(pathPdf, windreamIndexes, objectType, document, isNewDocument);

            if (!uploadFactura)
            {
                return "";
            }

            UpdateDocsNoFactura(windreamIndexes, objectType);

            return document.aName;
        }

        // --------------------------------------------------------------------------------------------------------------------------------------
        private void UpdateDocsNoFactura(WindreamIndexes windreamIndexes, WMObject objectType)
        {
            // Recuperamos los documentos que no son Factura con el mismo número de autorización
            var searchTermsNoFactura = BuildSearchTermsDiferenteFactura(windreamIndexes);
            var listaDocumentosNoFactura = BuscarDocumentos(objectType, searchTermsNoFactura);

            if (listaDocumentosNoFactura.Count > 0)
            {
                foreach (int docId in listaDocumentosNoFactura)
                {
                    WMObject docNoFactura;
                    try
                    {
                        docNoFactura = _wmSession!.GetWMObjectById(WMEntity.WMEntityDocument, docId);
                        if (!PrepareDocumentForEditing(docNoFactura))
                        {
                            _fileLogger.LogError($"No se pudo editar el documento en Windream. {docNoFactura.aName}");
                        }
                        else
                        {
                            _fileLogger.LogInformation($"Se actualiza el documento {docNoFactura.aName}");
                            UpdateDocumentIndexesNoFactura(docNoFactura, windreamIndexes);
                            docNoFactura.Save();
                            docNoFactura.unlock();
                        }
                    }
                    catch (Exception)
                    {
                        _fileLogger.LogError($"Documento no válido.");
                    }
                }
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------------------
        private void UpdateDocumentIndexesNoFactura(WMObject document, WindreamIndexes windreamIndexes)
        {


            if (!string.IsNullOrEmpty(windreamIndexes.NoFactura))
            {
                document.SetVariableValue("NoFactura", windreamIndexes.NoFactura);
                document.AddHistory($"NoFactura: {windreamIndexes.NoFactura}");
            }

            if (!string.IsNullOrEmpty(windreamIndexes.Cobertura))
            {
                document.SetVariableValue("Cobertura", windreamIndexes.Cobertura);
                document.AddHistory($"Cobertura: {windreamIndexes.Cobertura}");
            }

            if (!string.IsNullOrEmpty(windreamIndexes.NIFMutua))
            {
                document.SetVariableValue("NIFMutua", windreamIndexes.NIFMutua);
                document.AddHistory($"NIFMutua: {windreamIndexes.NIFMutua}");
            }

            if (!string.IsNullOrEmpty(windreamIndexes.NombrePaciente))
            {
                document.SetVariableValue("NombrePaciente", windreamIndexes.NombrePaciente);
                document.AddHistory($"NombrePaciente: {windreamIndexes.NombrePaciente}");
            }

            if (!string.IsNullOrEmpty(windreamIndexes.DNIPaciente))
            {
                document.SetVariableValue("DNIPaciente", windreamIndexes.DNIPaciente);
                document.AddHistory($"DNIPaciente: {windreamIndexes.DNIPaciente}");
            }

            if (windreamIndexes.FechaFactura.HasValue)
            {
                document.SetVariableValue("FechaFactura", windreamIndexes.FechaFactura);
                document.AddHistory($"FechaFactura: {windreamIndexes.FechaFactura}");
            }

        }

        // --------------------------------------------------------------------------------------------------------------------------------------
        private List<(string campo, string valor, OperadorBusqueda operador)> BuildSearchTermsFactura(WindreamIndexes windreamIndexes)
        {
            return new List<(string campo, string valor, OperadorBusqueda operador)>
            {
                ("NoFactura", windreamIndexes.NoFactura!, OperadorBusqueda.Igual),
                ("TipoDoc", windreamIndexes.TipoDoc.ToString()!, OperadorBusqueda.Igual)
            };
        }

        // --------------------------------------------------------------------------------------------------------------------------------------
        private List<(string campo, string valor, OperadorBusqueda operador)> BuildSearchTermsAut(WindreamIndexes windreamIndexes)
        {
            return new List<(string campo, string valor, OperadorBusqueda operador)>
            {
                ("NoAutorizacion", windreamIndexes.NoAutorizacion!, OperadorBusqueda.Igual),
                ("TipoDoc", windreamIndexes.TipoDoc.ToString()!, OperadorBusqueda.Igual)
            };
        }

        // --------------------------------------------------------------------------------------------------------------------------------------
        private List<(string campo, string valor, OperadorBusqueda operador)> BuildSearchTermsDiferenteFactura(WindreamIndexes windreamIndexes)
        {
            return new List<(string campo, string valor, OperadorBusqueda operador)>
            {
                ("NoAutorizacion", windreamIndexes.NoAutorizacion!, OperadorBusqueda.Igual),
                ("TipoDoc", "Factura", OperadorBusqueda.Diferente)
            };
        }


        // --------------------------------------------------------------------------------------------------------------------------------------
        private bool IsNewDocument(ArrayList listaDocumentos)
        {
            return listaDocumentos == null || listaDocumentos.Count == 0;
        }

        // --------------------------------------------------------------------------------------------------------------------------------------
        private WMObject CreateNewDocument(IWMSession2 wmSession2, WindreamIndexes windreamIndexes)
        {
            _fileLogger.LogInformation("No se encontró el documento en Windream, se procede a crearlo.");

            string targetFolder = CreateWindreamSubfolders();

            string matchingFileName;
            if (windreamIndexes.TipoDoc == TipoDocumento.Factura)
                matchingFileName = windreamIndexes.NoFactura! + "-F.pdf";
            else if (windreamIndexes.TipoDoc == TipoDocumento.Autorización)
                matchingFileName = windreamIndexes.NoAutorizacion! + "-A.pdf";
            else
            {
                if (string.IsNullOrEmpty(windreamIndexes.NoAutorizacion))
                    matchingFileName = Common.GenerateUniqueIdentifier() + "-" + "I.pdf";
                else
                    matchingFileName = windreamIndexes.NoAutorizacion! + "-" + Common.GenerateUniqueIdentifier() + "-" + "I.pdf";
            }
                

            // string matchingFileName = windreamIndexes.NoAutorizacion! + "F.pdf";
            string documentPath = Path.Combine(targetFolder, matchingFileName);

            return wmSession2.GetNewWMObjectFS(WMEntity.WMEntityDocument, documentPath, 0);
        }

        // --------------------------------------------------------------------------------------------------------------------------------------
        private WMObject CreateNewVersion(ArrayList listaDocumentos)
        {
            _fileLogger.LogInformation("Se encontró la factura en Windream, se procede a crear una nueva versión.");

            WMObject document = _wmSession!.GetWMObjectById(WMEntity.WMEntityDocument, (int)listaDocumentos[0]!);
            document.LockFor((int)WMObjectEditMode.WMObjectEditModeMakeVersion);
            document.CreateVersion();
            document.Save();
            document.unlock();

            return document;
        }


        // -------------------------------------------------------------------------------------------------------
        // FUNCTION: CreateDirectory
        // Función que crea un directorio en Windream
        // NBL 2024/04/17
        // -------------------------------------------------------------------------------------------------------
        public bool CreateDirectory(string directoryPath)
        {
            if (_wmSession == null)
            {
                throw new InvalidOperationException("La sesión de Windream no está inicializada.");
            }

            WMObject oWMObject;
            try
            {
                var wmSession6 = (IWMSession6)_wmSession;
                oWMObject = wmSession6.GetNewWMObjectFS(WMEntity.WMEntityFolder, directoryPath, (int)WMObjectEditMode.WMObjectEditModeNoEdit);
            }
            catch (Exception ex)
            {
                _fileLogger.LogError("Error al crear el directorio en Windream: " + ex.Message);
                return false;
            }

            return true;
        }

        // -------------------------------------------------------------------------------------------------------
        // FUNCTION: DirectoryExists
        // Función que comprueba si un directorio existe en Windream
        // NBL 2024/04/17
        // -------------------------------------------------------------------------------------------------------
        public bool DirectoryExists(string directoryPath)
        {
            if (_wmSession == null)
            {
                throw new InvalidOperationException("La sesión de Windream no está inicializada.");
            }

            WMObject? oDirectory;
            try
            {
                oDirectory = _wmSession.GetWMObjectByPath(WMEntity.WMEntityFolder, directoryPath);
            }
            catch (Exception ex)
            {
                _fileLogger.LogError("Error al comprobar si el directorio existe en Windream: " + ex.Message);
                return false;
            }

            return true;
        }

        // --------------------------------------------------------------------------------------------------------------------------------------
        private bool PrepareDocumentForEditing(WMObject document)
        {
            if (!document.IsEditableFor((int)WMObjectEditMode.WMObjectEditModeObjectAndRights))
            {
                _fileLogger.LogError("No se puede editar el documento en Windream.");
                return false;
            }

            if (!document.LockFor((int)WMObjectEditMode.WMObjectEditModeObjectAndRights))
            {
                _fileLogger.LogError("No se puede bloquear el documento en Windream.");
                return false;
            }

            return true;
        }

        // --------------------------------------------------------------------------------------------------------------------------------------
        private bool UploadPdfToWindream(string pathPdf, WindreamIndexes windreamIndexes, WMObject objectType, WMObject document, bool isNewDocument)
        {
            try
            {
                WMFileIO oFileIO = new WMFileIO();
                WMStream lobjWMStream = ((IWMObject2)document).OpenStream("BinaryObject", WMObjectStreamOpenMode.WMObjectStreamOpenModeReadWrite);
                oFileIO.aWMStream = (WMOTOOLLib.IWMStream)lobjWMStream;

                oFileIO.bstrOriginalFileName = pathPdf;
                oFileIO.ImportOriginal(true);

                if (isNewDocument)
                {
                    document.AddHistory($"Documento creado con el tipo {objectType.aName}");
                    document.aObjectType = objectType;
                    SetDocumentLifeCycle(document);
                }

                UpdateDocumentIndexes(document, windreamIndexes);
                document.Save();
                document.unlock();
            }
            catch (Exception ex)
            {
                if (document.aLocked)
                {
                    document.unlock();
                }
                _fileLogger.LogError("Error al subir el archivo a Windream: " + ex.Message);
                return false;
            }

            return true;
        }

        // --------------------------------------------------------------------------------------------------------------------------------------
        private void SetDocumentLifeCycle(WMObject document)
        {
            if (_serviceConfig.MonthsArchive > 0)
            {
                IWMObject2? oLifeCycle = document as IWMObject6;
                WMLifeCycle lifeCycle = oLifeCycle!.aWMLifeCycle;

                DateTime dtCurrent = DateTime.Now;
                DateTime finalDate = dtCurrent.AddMonths(_serviceConfig.MonthsArchive);

                lifeCycle.SetPeriodEndDate(WMLifeCycleType.WMLifeCycleTypeEditPeriod, finalDate);
                lifeCycle.SetPeriodEndDate(WMLifeCycleType.WMLifeCycleTypeArchivePeriod, finalDate.AddDays(1));
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------------------
        private void UpdateDocumentIndexes(WMObject document, WindreamIndexes windreamIndexes)
        {
            if (!string.IsNullOrEmpty(windreamIndexes.NoFactura))
            {
                document.SetVariableValue("NoFactura", windreamIndexes.NoFactura);
                document.AddHistory($"NoFactura: {windreamIndexes.NoFactura}");
            }

            if (!string.IsNullOrEmpty(windreamIndexes.NoAutorizacion))
            {
                document.SetVariableValue("NoAutorizacion", windreamIndexes.NoAutorizacion);
                document.AddHistory($"NoAutorizacion: {windreamIndexes.NoAutorizacion}");
            }

            if (!string.IsNullOrEmpty(windreamIndexes.Cobertura))
            {
                document.SetVariableValue("Cobertura", windreamIndexes.Cobertura);
                document.AddHistory($"Cobertura: {windreamIndexes.Cobertura}");
            }

            if (!string.IsNullOrEmpty(windreamIndexes.CoberturaInforme))
            {
                document.SetVariableValue("CoberturaInforme", windreamIndexes.CoberturaInforme);
                document.AddHistory($"CoberturaInforme: {windreamIndexes.CoberturaInforme}");
            }

            if (!string.IsNullOrEmpty(windreamIndexes.NIFMutua))
            {
                document.SetVariableValue("NIFMutua", windreamIndexes.NIFMutua);
                document.AddHistory($"NIFMutua: {windreamIndexes.NIFMutua}");
            }

            if (!string.IsNullOrEmpty(windreamIndexes.NombrePaciente))
            {
                document.SetVariableValue("NombrePaciente", windreamIndexes.NombrePaciente);
                document.AddHistory($"NombrePaciente: {windreamIndexes.NombrePaciente}");
            }

            if (!string.IsNullOrEmpty(windreamIndexes.DNIPaciente))
            {
                document.SetVariableValue("DNIPaciente", windreamIndexes.DNIPaciente);
                document.AddHistory($"DNIPaciente: {windreamIndexes.DNIPaciente}");
            }

            if (windreamIndexes.FechaFactura.HasValue)
            {
                document.SetVariableValue("FechaFactura", windreamIndexes.FechaFactura);
                document.AddHistory($"FechaFactura: {windreamIndexes.FechaFactura}");
            }

            if (windreamIndexes.TipoDoc != null)
            {
                document.SetVariableValue("TipoDoc", windreamIndexes.TipoDoc.ToString());
                document.AddHistory($"TipoDoc: {windreamIndexes.TipoDoc}");
            }
        }


        // ----------------------------------------------------------------------------------------------------------
        public ArrayList BuscarDocumentos(WMObject objectType, List<(string campo, string valor, OperadorBusqueda operador)> searchTerms)
        {
            // Buscamos si ya existe una factura con el mismo número de factura
            WMSearch wmSearch = _wmSession!.CreateWMSearch(WMEntity.WMEntityDocument);
            // Restringimos por el tipo de documento
            wmSearch.aWMObjectType = objectType;

            // Añadimos los términos de búsqueda
            foreach (var term in searchTerms)
            {
                if (term.operador == OperadorBusqueda.Igual)
                {
                    wmSearch.AddSearchTerm(term.campo, term.valor, WMSearchOperator.WMSearchOperatorEqual, WMSearchRelation.WMSearchRelationAnd, 0, 0);
                }
                else
                {
                    wmSearch.AddSearchTerm(term.campo, term.valor, WMSearchOperator.WMSearchOperatorNotEqual, WMSearchRelation.WMSearchRelationAnd, 0, 0);
                }
            }

            // Ejecutamos la búsqueda
            WMObjects lobjSearchResult = wmSearch.ExecuteEx(WMSearchMode.WMSearchModeNoCount | WMSearchMode.WMSearchModeValues);

            ArrayList lobjListaDocuments = new();
            string[] VariablesNames = { "szLongName", "dwDocID", "dwDocDBID" };
            int MAXFETCHCOUNT = 100000;
            Array ResultList;

            try
            {
                ResultList = (Array)lobjSearchResult.GetValues(MAXFETCHCOUNT, 0, VariablesNames);
                // Hacemos un bucle por cada uno
                for (int lintContador = 0; lintContador <= ResultList.GetUpperBound(1); lintContador++)
                {
                    lobjListaDocuments.Add(ResultList.GetValue(1, lintContador));
                }
            }
            catch (Exception)
            {
                // Si hay un error es que no hay datos para el número de petición dado
                _fileLogger.LogInformation("No hay documentos para el criterio");
            }

            return lobjListaDocuments;
        }

        // -------------------------------------------------------------------------------------------------------
        private string importNoFactura(string pathPdf, WindreamIndexes windreamIndexes, WMObject objectType, bool alwaysCreate = false)
        {
            // Solo tengo el número de autorización
            // Busco si hay algún documento con el mismo número de autorización
            var searchTerms = BuildSearchTermsAut(windreamIndexes);
            var listaDocumentos = BuscarDocumentos(objectType, searchTerms);

            WMObject document;
            IWMSession2? wmSession2 = _wmSession as IWMSession2;

            if (wmSession2 == null)
            {
                _fileLogger.LogError("No se pudo obtener la sesión de Windream.");
                return "";
            }

            // Desactivar el evento de indexación
            wmSession2.SwitchEvents((int)WMCOMEvent.WMCOMEventWMSessionNeedIndex, false);

            bool isNewDocument = false;

            if (IsNewDocument(listaDocumentos) || alwaysCreate)
            {
                document = CreateNewDocument(wmSession2, windreamIndexes);
                isNewDocument = true;
            }
            else
            {
                document = CreateNewVersion(listaDocumentos);
                isNewDocument = false;
            }

            if (!PrepareDocumentForEditing(document))
            {
                return "";
            }

            bool uploadAutorizacion = UploadPdfToWindream(pathPdf, windreamIndexes, objectType, document, isNewDocument);

            if (!uploadAutorizacion)
            {
                return "";
            }

            // Busco la factura con el mismo número de autorización, para actualizar indices
            WindreamIndexes? windreamIndexesFactura = GetWindreamIndexesFromFactura(windreamIndexes.NoAutorizacion!, objectType);

            if (windreamIndexesFactura != null)
            {
                if (PrepareDocumentForEditing(document))
                {
                    UpdateDocumentIndexesNoFactura(document, windreamIndexesFactura);
                    document.Save();
                    document.unlock();
                }
            }

            return document.aName;
        }

        // -------------------------------------------------------------------------------------------------------
        public WindreamIndexes? GetWindreamIndexesFromFactura(string noAutorizacion, WMObject wmObject)
        {
            var searchTerms = BuildSearchTermsAut(
                new WindreamIndexes
                {
                    NoAutorizacion = noAutorizacion,
                    TipoDoc = TipoDocumento.Factura
                }
            );
            var listaDocumentos = BuscarDocumentos(wmObject, searchTerms);

            foreach (var documento in listaDocumentos)
            {
                try
                {
                    WMObject document = _wmSession!.GetWMObjectById(WMEntity.WMEntityDocument, (int)documento!);

                    WindreamIndexes windreamIndexes = new WindreamIndexes
                    {
                        NoFactura = document.GetVariableValue("NoFactura") as string,
                        Cobertura = document.GetVariableValue("Cobertura") as string,
                        NIFMutua = document.GetVariableValue("NIFMutua") as string,
                        NombrePaciente = document.GetVariableValue("NombrePaciente") as string,
                        DNIPaciente = document.GetVariableValue("DNIPaciente") as string,
                        FechaFactura = document.GetVariableValue("FechaFactura") as DateTime?
                    };

                    return windreamIndexes;
                }
                catch (Exception ex)
                {
                    _fileLogger.LogError($"Error retrieving document: {ex.Message}");
                }
            }

            return null;
        }


        // -------------------------------------------------------------------------------------------------------
        // FUNCTION: CreateWindreamSubfolders
        // Función que crea una subcarpeta en Windream
        // NBL 2024/04/17
        // -------------------------------------------------------------------------------------------------------
        private string CreateWindreamSubfolders()
        {
            string folder1 = _serviceConfig.WindreamPath + "\\" + DateTime.Now.Year;
            string folder2 = folder1 + "\\" + DateTime.Now.Month.ToString().PadLeft(2, '0');

            if (!DirectoryExists(folder1))
            {
                CreateDirectory(folder1);
            }
            if (!DirectoryExists(folder2))
            {
                CreateDirectory(folder2);
            }

            return folder2;
        }

        // -------------------------------------------------------------------------------------------------------------------------------
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
                _fileLogger.LogError("Error de windream: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                // mobjWMMsgHandler.ComErrHRes = ex.GetHashCode();
                // mobjWMMsgHandler.ComErrDesc = ex.Message;
                // mobjWMMsgHandler.ShowError("COM-Exception: " + ex.GetHashCode().ToString("x"));
                _fileLogger.LogError("Error de windream: " + ex.Message);
                return false;
            }

            return false;
        }
    }
}
