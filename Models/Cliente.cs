using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Proyecto_taller.Models
{
    public class Cliente : INotifyPropertyChanged
    {
        private int clienteID;
        private string nombre = string.Empty;
        private string apellido = string.Empty;
        private string telefono = string.Empty;
        private string correo = string.Empty;
        private string direccion = string.Empty;
        private DateTime fechaRegistro = DateTime.Now;
        private ICollection<Vehiculo> vehiculos;

        public int ClienteID
        {
            get => clienteID;
            set
            {
                clienteID = value;
                OnPropertyChanged();
            }
        }

        public string Nombre
        {
            get => nombre;
            set
            {
                nombre = value;
                OnPropertyChanged();
            }
        }

        public string Apellido
        {
            get => apellido;
            set
            {
                apellido = value;
                OnPropertyChanged();
            }
        }

        public string Telefono
        {
            get => telefono;
            set
            {
                telefono = value;
                OnPropertyChanged();
            }
        }

        public string Correo
        {
            get => correo;
            set
            {
                correo = value;
                OnPropertyChanged();
            }
        }

        public string Direccion
        {
            get => direccion;
            set
            {
                direccion = value;
                OnPropertyChanged();
            }
        }

        public DateTime FechaRegistro
        {
            get => fechaRegistro;
            set
            {
                fechaRegistro = value;
                OnPropertyChanged();
            }
        }

        public ICollection<Vehiculo> Vehiculos
        {
            get => vehiculos;
            set
            {
                vehiculos = value;
                OnPropertyChanged();
            }
        }

        // Evento que notifica cambios a la vista
        public event PropertyChangedEventHandler PropertyChanged;

        // Método para invocar el evento cuando una propiedad cambia
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
