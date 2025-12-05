using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MonitorBolsaDeTrabajo.Services;
using MonitorBolsaDeTrabajo.Workers;
using MonitorBolsaDeTrabajo.Models;
using System.IO;
public class Program
{
    public static async Task Main(string[] args)
    {
        // Verificar si es ejecución única (para GitHub Actions)
        if (args.Contains("--run-once"))
        {
            await RunOnceAsync();
        }
        else
        {
            // Modo normal (para desarrollo local)
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }
    }

    private static async Task RunOnceAsync()
    {
        Console.WriteLine("=== MODO GITHUB ACTIONS ===");
        Console.WriteLine($"Iniciando verificación: {DateTime.Now}");
        
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"appsettings.Production.json", optional: true)
            .AddEnvironmentVariables()  // Para leer variables de GitHub Secrets
            .Build();
        
        // Configurar servicios
        var services = new ServiceCollection();
        ConfigureServices(services, config);
        var serviceProvider = services.BuildServiceProvider();
        
        // Ejecutar verificación
        var scraper = serviceProvider.GetRequiredService<IWebScraperService>();
        var emailService = serviceProvider.GetRequiredService<IEmailService>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        
        try
        {
            logger.LogInformation("Buscando nuevas ofertas...");
            var nuevasOfertas = await scraper.GetNuevasOfertasAsync();
            
            if (nuevasOfertas.Any())
            {
                logger.LogInformation($"Encontradas {nuevasOfertas.Count} nuevas ofertas");
                await emailService.SendNewOfertasEmailAsync(nuevasOfertas);
                
                // Guardar en el repositorio
                await SaveOfertasToRepo(scraper, nuevasOfertas);
            }
            else
            {
                logger.LogInformation("No hay nuevas ofertas");
            }
            
            logger.LogInformation("Proceso completado exitosamente");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante la ejecución");
            Environment.Exit(1); // Código de error para GitHub Actions
        }
    }

    private static async Task SaveOfertasToRepo(IWebScraperService scraper, List<Oferta> nuevasOfertas)
    {
        // Esto mantiene el archivo ofertas.json actualizado en el repositorio
        var ofertasActuales = await scraper.GetOfertasAsync();
        await scraper.SaveOfertasAsync(ofertasActuales);
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IConfiguration>(config);
        services.AddScoped<IWebScraperService, WebScraperService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddLogging(configure => 
        {
            configure.AddConsole();
            configure.AddDebug();
            configure.SetMinimumLevel(LogLevel.Information);
        });
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IConfiguration>(context.Configuration);
                services.AddScoped<IWebScraperService, WebScraperService>();
                services.AddScoped<IEmailService, EmailService>();
                services.AddHostedService<OfertaWorker>();
                services.AddLogging(configure => configure.AddConsole());
            });
}