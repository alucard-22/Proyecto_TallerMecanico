using Proyecto_taller.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto_taller.Helpers
{
    /// <summary>
    /// Gestiona la sesión del usuario actual
    /// </summary>
    public static class SessionManager
    {
        private static Usuario _usuarioActual;

        /// <summary>
        /// Usuario actualmente autenticado
        /// </summary>
        public static Usuario UsuarioActual
        {
            get => _usuarioActual;
            private set => _usuarioActual = value;
        }

        /// <summary>
        /// Verifica si hay un usuario autenticado
        /// </summary>
        public static bool EstaAutenticado => _usuarioActual != null;

        /// <summary>
        /// Verifica si el usuario actual es Administrador
        /// </summary>
        public static bool EsAdministrador => EstaAutenticado &&
            _usuarioActual.Rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Inicia sesión con un usuario
        /// </summary>
        public static void IniciarSesion(Usuario usuario)
        {
            if (usuario == null)
                throw new ArgumentNullException(nameof(usuario));

            UsuarioActual = usuario;
        }

        /// <summary>
        /// Cierra la sesión actual
        /// </summary>
        public static void CerrarSesion()
        {
            UsuarioActual = null;
        }

        /// <summary>
        /// Obtiene el nombre del usuario actual
        /// </summary>
        public static string ObtenerNombreUsuario()
        {
            return EstaAutenticado ? UsuarioActual.NombreCompleto : "Invitado";
        }

        /// <summary>
        /// Obtiene el rol del usuario actual
        /// </summary>
        public static string ObtenerRol()
        {
            return EstaAutenticado ? UsuarioActual.Rol : "Sin acceso";
        }
    }
}
