using Proyecto_taller.Data;
using Proyecto_taller.Models;
using System;

namespace Proyecto_taller.Helpers
{
    /// <summary>
    /// Punto único para registrar acciones de auditoría. Se usa el usuario
    /// actual de SessionManager por defecto, así que en la mayoría de los
    /// casos basta con llamar Registrar("Eliminar", "Cliente", id, detalle).
    /// Cualquier excepción al guardar la auditoría se atrapa internamente
    /// para que un fallo en el registro de auditoría nunca bloquee la
    /// operación principal que se está auditando.
    /// </summary>
    public static class AuditoriaHelper
    {
        public static void Registrar(string accion, string entidad, int? entidadId, string detalle)
        {
            try
            {
                using var db = new TallerDbContext();

                var registro = new RegistroAuditoria
                {
                    UsuarioID = SessionManager.UsuarioActual?.UsuarioID,
                    NombreUsuario = SessionManager.EstaAutenticado
                        ? SessionManager.UsuarioActual.NombreUsuario
                        : "Sistema",
                    Accion = accion,
                    Entidad = entidad,
                    EntidadID = entidadId,
                    Detalle = detalle,
                    Fecha = DateTime.Now
                };

                db.RegistrosAuditoria.Add(registro);
                db.SaveChanges();
            }
            catch
            {
                // No se interrumpe la operación principal si la auditoría falla.
                // En un sistema más robusto esto se enviaría a un log de archivo
                // como respaldo, pero para el alcance de este proyecto se omite.
            }
        }
    }
}
