using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

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

        // Guardará el JSON en la BD
        private string _permisosJson = "[]";

        public int UsuarioID
        {
            get => _usuarioID;
            set
            {
                _usuarioID = value;
                OnPropertyChanged();
            }
        }

        public string NombreUsuario
        {
            get => _nombreUsuario;
            set
            {
                _nombreUsuario = value;
                OnPropertyChanged();
            }
        }

        public string PasswordHash
        {
            get => _passwordHash;
            set
            {
                _passwordHash = value;
                OnPropertyChanged();
            }
        }

        public string NombreCompleto
        {
            get => _nombreCompleto;
            set
            {
                _nombreCompleto = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Roles disponibles: Administrador, Empleado
        /// </summary>
        public string Rol
        {
            get => _rol;
            set
            {
                _rol = value;
                OnPropertyChanged();
            }
        }

        public bool Activo
        {
            get => _activo;
            set
            {
                _activo = value;
                OnPropertyChanged();
            }
        }

        public DateTime FechaCreacion
        {
            get => _fechaCreacion;
            set
            {
                _fechaCreacion = value;
                OnPropertyChanged();
            }
        }

        public DateTime? UltimoAcceso
        {
            get => _ultimoAcceso;
            set
            {
                _ultimoAcceso = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Fecha del último cambio de contraseña
        /// </summary>
        public DateTime? FechaUltimoCambioPassword
        {
            get => _fechaUltimoCambioPassword;
            set
            {
                _fechaUltimoCambioPassword = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Campo almacenado en BD
        /// Ejemplo:
        /// ["Clientes","Inventario","Recibos"]
        /// </summary>
        public string PermisosJson
        {
            get => _permisosJson;
            set
            {
                _permisosJson = value ?? "[]";
                OnPropertyChanged();
                OnPropertyChanged(nameof(Permisos));
            }
        }

        /// <summary>
        /// Lista de permisos convertida desde JSON
        /// No se almacena directamente en BD
        /// </summary>
        [NotMapped]
        public List<string> Permisos
        {
            get
            {
                try
                {
                    return string.IsNullOrWhiteSpace(PermisosJson)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(PermisosJson)
                          ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }

            set
            {
                PermisosJson =
                    JsonSerializer.Serialize(
                        value ?? new List<string>()
                    );

                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName)
            );
        }
    }
}