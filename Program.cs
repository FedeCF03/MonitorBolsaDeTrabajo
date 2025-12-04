using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Console;
using MonitorBolsaDeTrabajo.Services;
using MonitorBolsaDeTrabajo.Workers;


var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Configuración
        services.AddSingleton<IConfiguration>(context.Configuration);
        
        // Servicios
        services.AddScoped<IWebScraperService, WebScraperService>();
        services.AddScoped<IEmailService, EmailService>();
        
        // Worker
        services.AddHostedService<OfertaWorker>();
    })
    .Build();

await host.RunAsync();