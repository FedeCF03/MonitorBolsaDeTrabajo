using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorBolsaDeTrabajo.Models
{
    public class Oferta
    {
        public required string Titulo { get; set; }
        public required string Link { get; set; }
        public required string OfertaPara { get; set; }
        public DateOnly Publicacion { get; set; }
        public DateOnly FechaDeCierre { get; set; }

    }
}