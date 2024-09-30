using PdfProcessingService.Models;
using PdfProcessingService.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintasoft.Imaging.Pdf.Tree.InteractiveForms;
using WINDREAMLib;
using WMCNNCTDLLLib;
using WMOBRWSLib;
using WMOMISCDLLLib;

namespace PdfProcessingService.Processors
{
    public sealed class WindreamImporter
    {
        // Declaramos las variables de módulo para la funcionalidad de Windream
        WMSession?_wmSession;
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
        public bool Import(string pathPdf, WindreamIndexes windreamIndexes, FileLogger fileLogger, ServiceConfig serviceConfig)
        {

            _fileLogger = fileLogger;
            _serviceConfig = serviceConfig;

            // Importar el PDF a Windream
            if (!Login2Windream())
            {
                _fileLogger.LogError("Error al conectar con Windream");
                return false;
            }

            // Recuperamos el object type 
            if (string.IsNullOrEmpty(_serviceConfig.ObjectType))
            {
                _fileLogger.LogError("El tipo de objeto no está configurado.");
                return false;
            }

            WMObject? oObjectType = GetDocumentType(_serviceConfig.ObjectType);
            if (oObjectType == null)
            {
                _fileLogger.LogError("No se ha encontrado el tipo de documento en Windream");
                return false;
            }

            // Dependiendo del tipo de documento, la operativa será diferente
            if (windreamIndexes.TipoDoc == TipoDocumento.Autorización)
            {
                // Si no tenemos el No de Autorización, no podemos trabajar con el documento
                if (String.IsNullOrEmpty(windreamIndexes.NoAutorizacion))
                {
                    _fileLogger.LogError("No se ha especificado el número de autorización.");
                    return false;
                }
                return importAutorizacion(pathPdf, windreamIndexes);
            }
            else if (windreamIndexes.TipoDoc == TipoDocumento.Factura)
            {
                // Si no tenemos el número de factura, no podemos trabajar con el documento
                if (String.IsNullOrEmpty(windreamIndexes.NoFactura))
                {
                    _fileLogger.LogError("No se ha especificado el número de factura.");
                    return false;
                }
                return importFactura(pathPdf, windreamIndexes, oObjectType);
            }
            else if (windreamIndexes.TipoDoc == TipoDocumento.Informe)
            {


            }
            else
            {
                _fileLogger.LogError("Tipo de documento no soportado.");
                return false;
            }
            return true;

        }
        // -------------------------------------------------------------------------------------------------------
        private bool importFactura(string pathPdf, WindreamIndexes windreamIndexes, WMObject objectType)
        {
            // Buscamos si ya existe una factura con el mismo número de factura
            WMSearch wmSearch = _wmSession!.CreateWMSearch(WMEntity.WMEntityDocument);
            // Restringimos por el tipo de documento
            wmSearch.aWMObjectType = objectType;
            wmSearch.AddSearchTerm("NoFactura", windreamIndexes.NoFactura, WMSearchOperator.WMSearchOperatorEqual, WMSearchRelation.WMSearchRelationAnd, 0, 0);
            wmSearch.AddSearchTerm("TipoDoc", windreamIndexes.TipoDoc.ToString(), WMSearchOperator.WMSearchOperatorEqual, WMSearchRelation.WMSearchRelationAnd, 0, 0);
            // Ejecutamos la búsqueda

            return true;
        }

        // -------------------------------------------------------------------------------------------------------
        private bool importAutorizacion(string pathPdf, WindreamIndexes windreamIndexes)
        {
            return true;
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

                if (lstrServidor != null && lstrServidor.Length > 0 )
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

        // -------------------------------------------------------------------------------------------------------------------------------
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
    }
}
