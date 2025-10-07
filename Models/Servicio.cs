using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto_taller.Models
{
    public class Servicio
    {
        public int ServicioID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string Categoria { get; set; } = "Otro";
        public decimal CostoBase { get; set; }
        public ICollection<Trabajos_Servicios>? TrabajosServicios { get; set; }
    }
}
