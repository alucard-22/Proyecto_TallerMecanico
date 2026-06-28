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

        // ── NUEVO: anticipo / adelanto que el cliente deja al ingresar el
        // vehículo. Se descuenta automáticamente del total a cobrar cuando
        // el trabajo se finaliza, para no cobrarle dos veces al cliente.
        public decimal Anticipo { get; set; } = 0;

        // ── NUEVO: empleado (Usuario) asignado para realizar este trabajo.
        // Permite saber quién es responsable de cada orden y filtrar el
        // listado de trabajos por empleado, tanto para control operativo
        // como para que un administrador pueda revisar la carga de trabajo
        // de cada persona.
        public int? UsuarioAsignadoID { get; set; }

        public Vehiculo? Vehiculo { get; set; }
        public Usuario? UsuarioAsignado { get; set; }
        public ICollection<Trabajos_Servicios>? Servicios { get; set; }
        public ICollection<Trabajos_Repuestos>? Repuestos { get; set; }
        public ICollection<Pago>? Pagos { get; set; }
    }
}
