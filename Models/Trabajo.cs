using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto_taller.Models
{
    public class Trabajo
    {
        public int TrabajoID { get; set; }
        public int VehiculoID { get; set; }
        public DateTime FechaIngreso { get; set; } = DateTime.Now;
        public DateTime? FechaEntrega { get; set; }
        public string? Descripcion { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public string TipoTrabajo { get; set; } = "Mecánica";
        public decimal? PrecioEstimado { get; set; }
        public decimal? PrecioFinal { get; set; }

        public Vehiculo? Vehiculo { get; set; }
        public ICollection<Trabajos_Servicios>? Servicios { get; set; }
        public ICollection<Trabajos_Repuestos>? Repuestos { get; set; }
        public ICollection<Pago>? Pagos { get; set; }
    }
}
