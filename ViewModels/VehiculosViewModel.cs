using Proyecto_taller.Data;
using Proyecto_taller.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.ViewModels
{
    public class VehiculosViewModel : INotifyPropertyChanged
    {
        private Vehiculo _vehiculoSeleccionado;
        public ObservableCollection<Vehiculo> Vehiculos { get; set; }
        public Vehiculo VehiculoSeleccionado
        {
            get => _vehiculoSeleccionado;
            set
            {
                _vehiculoSeleccionado = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }
        public ICommand CargarVehiculosCommand { get; }
        public ICommand AgregarVehiculoCommand { get; }
        public ICommand EditarVehiculoCommand { get; }
        public ICommand EliminarVehiculoCommand { get; }
        public VehiculosViewModel()
        {
            Vehiculos = new ObservableCollection<Vehiculo>();
            CargarVehiculosCommand = new RelayCommand(CargarVehiculos);
            AgregarVehiculoCommand = new RelayCommand(AgregarVehiculo);
            EditarVehiculoCommand = new RelayCommand(EditarVehiculo, () => VehiculoSeleccionado != null);
            EliminarVehiculoCommand = new RelayCommand(EliminarVehiculo, () => VehiculoSeleccionado != null);
            CargarVehiculos();
        }
        private void CargarVehiculos()
        {
            Vehiculos.Clear();
            using var db = new TallerDbContext();
            var vehiculos = db.Vehiculos
            .Include(v => v.Cliente) // Cargar datos del cliente relacionado
            .ToList();
            foreach (var vehiculo in vehiculos)
            {
                Vehiculos.Add(vehiculo);
            }
        }
        private void AgregarVehiculo()
        {
            using var db = new TallerDbContext();

            // Obtener el primer cliente para asignarlo por defecto
            var primerCliente = db.Clientes.FirstOrDefault();

            if (primerCliente == null)
            {
                System.Windows.MessageBox.Show(
                    "Debe registrar al menos un cliente antes de agregar vehículos.",
                    "Sin Clientes",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var nuevo = new Vehiculo
            {
                Marca = "Toyota",
                Modelo = "Corolla",
                Anio = 2020,
                Placa = "0000-ABC",
                ClienteID = primerCliente.ClienteID
            };

            db.Vehiculos.Add(nuevo);
            db.SaveChanges();

            // Recargar con el cliente incluido
            var vehiculos = db.Vehiculos
                .Include(v => v.Cliente)
                .FirstOrDefault(v => v.VehiculoID == nuevo.VehiculoID);

            if (vehiculos != null)
            {
                Vehiculos.Add(vehiculos);
            }
        }
        private void EditarVehiculo()
        {
            if (VehiculoSeleccionado == null) return;

            System.Windows.MessageBox.Show(
                $"Editando vehículo: {VehiculoSeleccionado.Marca} {VehiculoSeleccionado.Modelo} - Placa: {VehiculoSeleccionado.Placa}",
                "Editar Vehículo");
        }

        private void EliminarVehiculo()
        {
            if (VehiculoSeleccionado == null) return;

            var resultado = System.Windows.MessageBox.Show(
                $"¿Está seguro de eliminar el vehículo {VehiculoSeleccionado.Marca} {VehiculoSeleccionado.Modelo} (Placa: {VehiculoSeleccionado.Placa})?",
                "Confirmar Eliminación",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (resultado == System.Windows.MessageBoxResult.Yes)
            {
                using var db = new TallerDbContext();
                var vehiculo = db.Vehiculos.Find(VehiculoSeleccionado.VehiculoID);
                if (vehiculo != null)
                {
                    db.Vehiculos.Remove(vehiculo);
                    db.SaveChanges();
                    Vehiculos.Remove(VehiculoSeleccionado);
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
