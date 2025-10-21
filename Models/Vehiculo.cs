using Proyecto_taller.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto_taller.Models
{
    public class Vehiculo
    {
        public int VehiculoID { get; set; }
        public int ClienteID { get; set; } 
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int? Anio { get; set; }
        public string Placa { get; set; } = string.Empty;

        // Navegación
        public Cliente Cliente { get; set; }
        public ICollection<Trabajo>? Trabajos { get; set; }
    }
}
