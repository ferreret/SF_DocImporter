using PdfProcessingService;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using System.Runtime.Versioning;
using LibDataExtractor;
using LibWin;

[assembly: SupportedOSPlatform("windows")]


var builder = Host.CreateApplicationBuilder(args);


builder.Services.AddWindowsService(
    options =>
    {
        options.ServiceName = "PdfProcessingService";
    }
);

LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);

builder.Services.AddSingleton<MetaDataExtractor>();
builder.Services.AddSingleton<WindreamImporter>();
builder.Services.AddHostedService<Worker>();


var host = builder.Build();
host.Run();
