using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MonitorBolsaDeTrabajo.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace MonitorBolsaDeTrabajo.Workers
{


public class OfertaWorker(
    ILogger<OfertaWorker> logger,
    IServiceProvider serviceProvider,
    IConfiguration configuration) : BackgroundService
{
    private readonly ILogger<OfertaWorker> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IConfiguration _configuration = configuration;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = _configuration.GetValue<int>("ScrapingSettings:CheckIntervalHours", 24);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Iniciando búsqueda de nuevas ofertas...");
                
                using var scope = _serviceProvider.CreateScope();
                var scraper = scope.ServiceProvider.GetRequiredService<IWebScraperService>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                
                var nuevasOfertas = await scraper.GetNuevasOfertasAsync();
                
                if (nuevasOfertas.Any())
                {
                    _logger.LogInformation($"Se encontraron {nuevasOfertas.Count} nuevas ofertas");
                    await emailService.SendNewOfertasEmailAsync(nuevasOfertas);
                }
                else
                {
                    _logger.LogInformation("No hay nuevas ofertas");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en la ejecución del worker");
            }
            
            // Esperar hasta la próxima ejecución (24 horas por defecto)
            await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
        }
    }
}
}