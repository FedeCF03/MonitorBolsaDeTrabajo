using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorBolsaDeTrabajo.Services
{
    public interface IEmailService
    {
            Task SendNewOfertasEmailAsync(List<Models.Oferta> ofertas);
    }
}