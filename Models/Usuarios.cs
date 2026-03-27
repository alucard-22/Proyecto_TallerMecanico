using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto_taller.Models
{
    public class Usuario : INotifyPropertyChanged
    {
        private int _usuarioID;
        private string _nombreUsuario = string.Empty;
        private string _passwordHash = string.Empty;
        private string _nombreCompleto = string.Empty;
        private string _rol = "Empleado";
        private bool _activo = true;
        private DateTime _fechaCreacion = DateTime.Now;
        private DateTime? _ultimoAcceso;
        private DateTime? _fechaUltimoCambioPassword;

        public int UsuarioID
        {
            get => _usuarioID;
            set { _usuarioID = value; OnPropertyChanged(); }
        }

        public string NombreUsuario
        {
            get => _nombreUsuario;
            set { _nombreUsuario = value; OnPropertyChanged(); }
        }

        public string PasswordHash
        {
            get => _passwordHash;
            set { _passwordHash = value; OnPropertyChanged(); }
        }

        public string NombreCompleto
        {
            get => _nombreCompleto;
            set { _nombreCompleto = value; OnPropertyChanged(); }
        }

        /// <summary>Roles disponibles: Administrador, Empleado</summary>
        public string Rol
        {
            get => _rol;
            set { _rol = value; OnPropertyChanged(); }
        }

        public bool Activo
        {
            get => _activo;
            set { _activo = value; OnPropertyChanged(); }
        }

        public DateTime FechaCreacion
        {
            get => _fechaCreacion;
            set { _fechaCreacion = value; OnPropertyChanged(); }
        }

        public DateTime? UltimoAcceso
        {
            get => _ultimoAcceso;
            set { _ultimoAcceso = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Fecha del último cambio de contraseña.
        /// Se actualiza cada vez que CambiarPasswordWindow guarda exitosamente.
        /// Permite auditar cuándo fue el último cambio sin guardar el historial completo.
        /// </summary>
        public DateTime? FechaUltimoCambioPassword
        {
            get => _fechaUltimoCambioPassword;
            set { _fechaUltimoCambioPassword = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
