using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonitorBolsaDeTrabajo.Models;

namespace MonitorBolsaDeTrabajo.Services
{
    public interface IWebScraperService
    {
        Task<List<Oferta>> GetOfertasAsync();
    Task<List<Oferta>> GetNuevasOfertasAsync();
    Task SaveOfertasAsync(List<Oferta> ofertas);
    }

    
}