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
        private int usuarioID;
        private string nombreUsuario = string.Empty;
        private string passwordHash = string.Empty;
        private string nombreCompleto = string.Empty;
        private string rol = "Empleado";
        private bool activo = true;
        private DateTime fechaCreacion = DateTime.Now;
        private DateTime? ultimoAcceso;

        public int UsuarioID
        {
            get => usuarioID;
            set
            {
                usuarioID = value;
                OnPropertyChanged();
            }
        }

        public string NombreUsuario
        {
            get => nombreUsuario;
            set
            {
                nombreUsuario = value;
                OnPropertyChanged();
            }
        }

        public string PasswordHash
        {
            get => passwordHash;
            set
            {
                passwordHash = value;
                OnPropertyChanged();
            }
        }

        public string NombreCompleto
        {
            get => nombreCompleto;
            set
            {
                nombreCompleto = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Roles disponibles: Administrador, Empleado
        /// </summary>
        public string Rol
        {
            get => rol;
            set
            {
                rol = value;
                OnPropertyChanged();
            }
        }

        public bool Activo
        {
            get => activo;
            set
            {
                activo = value;
                OnPropertyChanged();
            }
        }

        public DateTime FechaCreacion
        {
            get => fechaCreacion;
            set
            {
                fechaCreacion = value;
                OnPropertyChanged();
            }
        }

        public DateTime? UltimoAcceso
        {
            get => ultimoAcceso;
            set
            {
                ultimoAcceso = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
