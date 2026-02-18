using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto_taller.Models
{
    public class Factura
    {
        public int FacturaID { get; set; }
        public int TrabajoID { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public DateTime FechaEmision { get; set; } = DateTime.Now;
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; } = 0;
        public decimal Total { get; set; }
        public string? RazonSocial { get; set; }
        public string Estado { get; set; } = "Pagada"; // Pagada, Pendiente, Anulada

        // Navegación
        public Trabajo? Trabajo { get; set; }
    }
}
