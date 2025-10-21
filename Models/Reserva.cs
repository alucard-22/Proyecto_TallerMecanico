using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto_taller.Models
{
    public class Reserva
    {
        public int ReservaID { get; set; }
        public int ClienteID { get; set; }
        public int? VehiculoID { get; set; }
        public DateTime FechaReserva { get; set; } = DateTime.Now;
        public DateTime FechaHoraCita { get; set; }
        public string TipoServicio { get; set; } = "Mecánica";
        public string? Observaciones { get; set; }
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Confirmada, Cancelada, Completada

        // Navegación
        public Cliente? Cliente { get; set; }
        public Vehiculo? Vehiculo { get; set; }
    }
}
