using Proyecto_taller.Models;
using System;
using System.Collections.Generic;

namespace Proyecto_taller.Helpers
{
    public static class SessionManager
    {
        private static Usuario _usuarioActual;

        public static Usuario UsuarioActual
        {
            get => _usuarioActual;
            private set => _usuarioActual = value;
        }

        public static bool EstaAutenticado => _usuarioActual != null;

        public static bool EsAdministrador => EstaAutenticado &&
            _usuarioActual.Rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase);

        public static void IniciarSesion(Usuario usuario)
        {
            if (usuario == null)
                throw new ArgumentNullException(nameof(usuario));
            UsuarioActual = usuario;
        }

        public static void CerrarSesion() => UsuarioActual = null;

        public static string ObtenerNombreUsuario()
            => EstaAutenticado ? UsuarioActual.NombreCompleto : "Invitado";

        public static string ObtenerRol()
            => EstaAutenticado ? UsuarioActual.Rol : "Sin acceso";

        /// <summary>
        /// Verifica si el usuario actual tiene permiso para un módulo.
        /// Los administradores siempre tienen acceso a todo.
        /// </summary>
        public static bool TienePermiso(string modulo)
        {
            if (!EstaAutenticado) return false;
            if (EsAdministrador) return true;
            return UsuarioActual.Permisos.Contains(modulo);
        }

        // Nombres de módulos disponibles
        public static readonly List<string> ModulosDisponibles = new()
        {
            "Inicio",
            "Clientes",
            "Vehiculos",
            "Trabajos",
            "Reservas",
            "Inventario",
            "Recibos",
            "Reportes"
        };
    }
}