using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto_taller.Models
{
    public class Pago
    {
        public int PagoID { get; set; }
        public int TrabajoID { get; set; }
        public decimal Monto { get; set; }
        public string? MetodoPago { get; set; }
        public DateTime FechaPago { get; set; } = DateTime.Now;
        public Trabajo? Trabajo { get; set; }
    }
}
