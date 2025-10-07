using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto_taller.Models
{
    public class Trabajos_Servicios
    {
        public int TrabajoID { get; set; }
        public int ServicioID { get; set; }
        public int Cantidad { get; set; }
        public decimal Subtotal { get; set; }

        public Trabajo? Trabajo { get; set; }
        public Servicio? Servicio { get; set; }
    }
}
