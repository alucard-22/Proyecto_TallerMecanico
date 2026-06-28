using System;

namespace Proyecto_taller.Models
{
    /// <summary>
    /// Registro de auditoría: deja constancia de qué usuario realizó qué
    /// acción crítica y cuándo. No se audita absolutamente todo el sistema
    /// (eso generaría una tabla enorme sin valor práctico) — solo las
    /// acciones de alto impacto: login, eliminaciones, reinicio de base de
    /// datos, cambios de configuración, y asignación/finalización de
    /// trabajos importantes para el control del taller.
    /// </summary>
    public class RegistroAuditoria
    {
        public int RegistroAuditoriaID { get; set; }

        public int? UsuarioID { get; set; }

        /// <summary>Nombre de usuario al momento de la acción, guardado como
        /// texto independiente de la FK, para que el registro siga siendo
        /// legible aunque el usuario sea eliminado más adelante.</summary>
        public string NombreUsuario { get; set; } = string.Empty;

        /// <summary>Acción realizada, ej: "Eliminar", "Crear", "Reiniciar BD",
        /// "Finalizar Trabajo", "Asignar Empleado", "Login".</summary>
        public string Accion { get; set; } = string.Empty;

        /// <summary>Entidad afectada, ej: "Cliente", "Trabajo", "BaseDeDatos".</summary>
        public string Entidad { get; set; } = string.Empty;

        /// <summary>ID del registro afectado, si aplica (puede ser null para
        /// acciones globales como "Reiniciar BD" o "Login").</summary>
        public int? EntidadID { get; set; }

        /// <summary>Detalle legible de la acción, ej: "Cliente 'Juan Pérez' eliminado".</summary>
        public string Detalle { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.Now;

        public Usuario? Usuario { get; set; }
    }
}
