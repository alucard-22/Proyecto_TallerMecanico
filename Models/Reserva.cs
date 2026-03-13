using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto_taller.Models
{
    public class Reserva
    {
        public int ReservaID { get; set; }

        [Required]
        public int VehiculoID { get; set; } // ⭐ Solo VehiculoID, el cliente viene del vehículo

        [Required]
        public DateTime FechaReserva { get; set; } = DateTime.Now;

        [Required]
        public DateTime FechaHoraCita { get; set; }

        [Required]
        [StringLength(100)]
        public string TipoServicio { get; set; } = "Mecánica";

        [StringLength(500)]
        public string? Observaciones { get; set; }

        /// <summary>
        /// Estados posibles:
        /// - Pendiente: Nueva reserva, esperando confirmación
        /// - Confirmada: Cliente confirmó asistencia
        /// - En Curso: Cliente llegó, trabajo iniciado
        /// - Completada: Trabajo terminado
        /// - Cancelada: Cliente canceló
        /// - No Asistió: Cliente no se presentó
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Estado { get; set; } = "Pendiente";

        /// <summary>
        /// Prioridad de la reserva
        /// - Normal: Reserva regular
        /// - Urgente: Requiere atención inmediata
        /// - VIP: Cliente preferencial
        /// </summary>
        [StringLength(20)]
        public string Prioridad { get; set; } = "Normal";

        public DateTime? FechaConfirmacion { get; set; }
        public DateTime? FechaCompletado { get; set; }
        public DateTime? FechaCancelacion { get; set; }

        [StringLength(500)]
        public string? MotivoCancelacion { get; set; }

        public decimal? PrecioEstimado { get; set; }

        // Relaciones
        public Vehiculo? Vehiculo { get; set; }

        // ⭐ El cliente se obtiene a través de: Reserva.Vehiculo.Cliente
        // No se necesita una navegación directa a Cliente
    }
}
