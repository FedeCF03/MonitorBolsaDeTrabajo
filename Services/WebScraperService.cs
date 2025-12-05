using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using MonitorBolsaDeTrabajo.Models;

namespace MonitorBolsaDeTrabajo.Services
{
    public class WebScraperService : IWebScraperService
{
    private readonly IConfiguration _configuration;
    private readonly string _storagePath;
    private readonly string _baseUrl;

    public WebScraperService(IConfiguration configuration)
    {
        _configuration = configuration;
        _storagePath = Path.Combine(
            Directory.GetCurrentDirectory(), 
            _configuration["ScrapingSettings:StorageFilePath"] ?? "ofertas.json");
        _baseUrl = _configuration["ScrapingSettings:BaseUrl"];
    }

    public async Task<List<Oferta>> GetOfertasAsync()
    {
        var ofertas = new List<Oferta>();
        
        try
        {
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(_baseUrl);
            
            // Ajusta estos selectores según la estructura actual de la página
            var ofertaNodes = doc.DocumentNode.SelectNodes("//tbody/tr");

            if (ofertaNodes != null)
            {
                foreach (var node in ofertaNodes)
                {
                    var tds = node.SelectNodes("td");
                    if (tds != null && tds.Count >= 5)
                    {
                        var titulo = tds[0].InnerText.Trim();
                        var ofertaPara = tds[1].InnerText.Trim();
                        var publicacionStr = tds[2].InnerText.Trim();
                        var fechaDeCierreStr = tds[3].InnerText.Trim();
                        var link = tds[4].SelectSingleNode("a")?.GetAttributeValue("href", "") ?? "";

                        DateOnly publicacion;
                        DateOnly fechaDeCierre;
                        if (DateOnly.TryParse(publicacionStr, CultureInfo.GetCultureInfo("es-ES"), out publicacion) &&
                            DateOnly.TryParse(fechaDeCierreStr, CultureInfo.GetCultureInfo("es-ES"), out fechaDeCierre))
                        {
                            var oferta = new Oferta
                            {   
                                
                                Titulo = titulo,
                                OfertaPara = ofertaPara,
                                Publicacion = publicacion,
                                FechaDeCierre = fechaDeCierre,
                                Link = GetAbsoluteUrl(link)
                            };

                            ofertas.Add(oferta);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener ofertas: {ex.Message}");
        }
        
        return ofertas;
    }
    
    private string GetAbsoluteUrl(string relativeUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl))
            return _baseUrl;
            
        if (relativeUrl.StartsWith("http"))
            return relativeUrl;
            
        return $"https://bolsa.info.unlp.edu.ar{relativeUrl}";
    }

    public async Task<List<Oferta>> GetNuevasOfertasAsync()
    {
        var ofertasActuales = await GetOfertasAsync();
        var ofertasGuardadas = await LoadOfertasGuardadasAsync();
        
        // Filtrar solo las ofertas nuevas
        var nuevasOfertas = ofertasActuales
            .Where(o => !ofertasGuardadas.Any(og => og.Equals(o)))
            .ToList();
        // Guardar todas las ofertas actuales
        await SaveOfertasAsync(ofertasActuales);
        
        return nuevasOfertas;
    }

    public async Task SaveOfertasAsync(List<Oferta> ofertas)
    {
        var json = JsonSerializer.Serialize(ofertas, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        await File.WriteAllTextAsync(_storagePath, json);
    }
    
    private async Task<List<Oferta>> LoadOfertasGuardadasAsync()
    {
        if (!File.Exists(_storagePath))
            return new List<Oferta>();
        
        try
        {
            var json = await File.ReadAllTextAsync(_storagePath);
            return JsonSerializer.Deserialize<List<Oferta>>(json) ?? new List<Oferta>();
        }
        catch
        {
            return new List<Oferta>();
        }
    }
}
}