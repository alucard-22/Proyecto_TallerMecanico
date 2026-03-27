using Microsoft.EntityFrameworkCore;
using Proyecto_taller.Data;
using Proyecto_taller.Models;
using Proyecto_taller.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Proyecto_taller.ViewModels
{
    public class VehiculosViewModel : INotifyPropertyChanged
    {
        // ── Estado ────────────────────────────────────────────────────────────
        private Vehiculo? _vehiculoSeleccionado;
        private string _textoBusqueda = string.Empty;
        private int _totalVehiculos;

        private ObservableCollection<Vehiculo> _todosMaestro = new();
        public ObservableCollection<Vehiculo> Vehiculos { get; set; } = new();

        // ── Propiedades ───────────────────────────────────────────────────────

        public Vehiculo? VehiculoSeleccionado
        {
            get => _vehiculoSeleccionado;
            set
            {
                _vehiculoSeleccionado = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                _textoBusqueda = value;
                OnPropertyChanged();
                AplicarBusqueda();
            }
        }

        public int TotalVehiculos
        {
            get => _totalVehiculos;
            set { _totalVehiculos = value; OnPropertyChanged(); }
        }

        // ── Comandos ──────────────────────────────────────────────────────────

        public ICommand CargarVehiculosCommand { get; }
        public ICommand AgregarVehiculoCommand { get; }
        public ICommand EditarVehiculoCommand { get; }
        public ICommand VerHistorialCommand { get; }
        public ICommand EliminarVehiculoCommand { get; }
        public ICommand LimpiarBusquedaCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────

        public VehiculosViewModel()
        {
            CargarVehiculosCommand = new RelayCommand(CargarVehiculos);
            AgregarVehiculoCommand = new RelayCommand(AgregarVehiculo);
            EditarVehiculoCommand = new RelayCommand(EditarVehiculo,
                                        () => VehiculoSeleccionado != null);
            VerHistorialCommand = new RelayCommand(VerHistorial,
                                        () => VehiculoSeleccionado != null);
            EliminarVehiculoCommand = new RelayCommand(EliminarVehiculo,
                                        () => VehiculoSeleccionado != null);
            LimpiarBusquedaCommand = new RelayCommand(() => TextoBusqueda = string.Empty);

            CargarVehiculos();
        }

        // ── Carga y búsqueda ──────────────────────────────────────────────────

        public void CargarVehiculos()
        {
            var idAnterior = VehiculoSeleccionado?.VehiculoID;
            _todosMaestro.Clear();

            using var db = new TallerDbContext();
            var lista = db.Vehiculos
                .Include(v => v.Cliente)
                .OrderBy(v => v.Placa)
                .ToList();

            foreach (var v in lista)
                _todosMaestro.Add(v);

            TotalVehiculos = lista.Count;
            AplicarBusqueda();

            if (idAnterior.HasValue)
                VehiculoSeleccionado =
                    Vehiculos.FirstOrDefault(v => v.VehiculoID == idAnterior.Value);
        }

        private void AplicarBusqueda()
        {
            var texto = TextoBusqueda.Trim().ToLower();
            var query = _todosMaestro.AsEnumerable();

            if (!string.IsNullOrEmpty(texto))
                query = query.Where(v =>
                    v.Placa.ToLower().Contains(texto)
                    || v.Marca.ToLower().Contains(texto)
                    || v.Modelo.ToLower().Contains(texto)
                    || (v.Anio.HasValue && v.Anio.ToString()!.Contains(texto))
                    || (v.Cliente?.Nombre.ToLower().Contains(texto) ?? false)
                    || (v.Cliente?.Apellido.ToLower().Contains(texto) ?? false)
                    || (v.Cliente?.Telefono.Contains(texto) ?? false));

            Vehiculos.Clear();
            foreach (var v in query) Vehiculos.Add(v);
        }

        // ── Agregar vehículo ──────────────────────────────────────────────────
        // FIX: antes asignaba siempre el primer cliente de la BD.
        // Ahora abre EditarVehiculoWindow en modo nuevo con selector real de cliente.

        private void AgregarVehiculo()
        {
            var win = new EditarVehiculoWindow();
            if (win.ShowDialog() == true)
                CargarVehiculos();
        }

        // ── Editar vehículo ───────────────────────────────────────────────────
        // FIX: antes solo mostraba un MessageBox informativo.
        // Ahora abre EditarVehiculoWindow con los datos precargados.

        private void EditarVehiculo()
        {
            if (VehiculoSeleccionado == null) return;

            // Recargar desde BD para obtener datos frescos
            using var db = new TallerDbContext();
            var vehiculo = db.Vehiculos
                .Include(v => v.Cliente)
                .FirstOrDefault(v => v.VehiculoID == VehiculoSeleccionado.VehiculoID);

            if (vehiculo == null)
            {
                MessageBox.Show("El vehículo ya no existe en la base de datos.",
                    "No encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                CargarVehiculos();
                return;
            }

            var win = new EditarVehiculoWindow(vehiculo);
            if (win.ShowDialog() == true)
                CargarVehiculos();
        }

        // ── Historial de trabajos ─────────────────────────────────────────────

        private void VerHistorial()
        {
            if (VehiculoSeleccionado == null) return;
            var win = new HistorialVehiculoWindow(VehiculoSeleccionado.VehiculoID);
            win.ShowDialog();
        }

        // ── Eliminar vehículo ─────────────────────────────────────────────────

        private void EliminarVehiculo()
        {
            if (VehiculoSeleccionado == null) return;

            var resultado = MessageBox.Show(
                $"¿Eliminar el vehículo {VehiculoSeleccionado.Marca} " +
                $"{VehiculoSeleccionado.Modelo} (Placa: {VehiculoSeleccionado.Placa})?\n\n" +
                $"⚠️  Se eliminarán también todos los trabajos y reservas asociados.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado != MessageBoxResult.Yes) return;

            try
            {
                using var db = new TallerDbContext();
                var vehiculo = db.Vehiculos.Find(VehiculoSeleccionado.VehiculoID);
                if (vehiculo != null)
                {
                    db.Vehiculos.Remove(vehiculo);
                    db.SaveChanges();
                }

                CargarVehiculos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}