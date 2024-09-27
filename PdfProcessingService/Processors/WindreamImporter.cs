using PdfProcessingService.Models;
using PdfProcessingService.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public bool Import(string pathPdf, WindreamIndexes windreamIndexes, FileLogger fileLogger, ServiceConfig serviceConfig)
        {
            _fileLogger = fileLogger;
            _serviceConfig = serviceConfig;

            // Importar el PDF a Windream

            return true;
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
                    Type lobjSrvType = Type.GetTypeFromProgID("Windream.WMSession", lstrServidor, true);
                    _wmSession = Activator.CreateInstance(lobjSrvType) as WMSession;
                    _wmConnect.ServerName = lstrServidor;

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
