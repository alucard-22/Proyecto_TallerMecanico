using Proyecto_taller.Data;
using Proyecto_taller.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.ViewModels
{
    public class ReservasViewModel : INotifyPropertyChanged
    {
        private Reserva _reservaSeleccionada;
        private bool _filtroTodas = true;
        private bool _filtroPendientes;
        private bool _filtroConfirmadas;
        private bool _filtroHoy;

        public ObservableCollection<Reserva> Reservas { get; set; }

        public Reserva ReservaSeleccionada
        {
            get => _reservaSeleccionada;
            set
            {
                _reservaSeleccionada = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // Propiedades para los filtros
        public bool FiltroTodas
        {
            get => _filtroTodas;
            set
            {
                _filtroTodas = value;
                OnPropertyChanged();
                if (value) AplicarFiltro("Todas");
            }
        }

        public bool FiltroPendientes
        {
            get => _filtroPendientes;
            set
            {
                _filtroPendientes = value;
                OnPropertyChanged();
                if (value) AplicarFiltro("Pendiente");
            }
        }

        public bool FiltroConfirmadas
        {
            get => _filtroConfirmadas;
            set
            {
                _filtroConfirmadas = value;
                OnPropertyChanged();
                if (value) AplicarFiltro("Confirmada");
            }
        }

        public bool FiltroHoy
        {
            get => _filtroHoy;
            set
            {
                _filtroHoy = value;
                OnPropertyChanged();
                if (value) AplicarFiltro("Hoy");
            }
        }

        public ICommand CargarReservasCommand { get; }
        public ICommand AgregarReservaCommand { get; }
        public ICommand ConfirmarReservaCommand { get; }
        public ICommand CancelarReservaCommand { get; }
        public ICommand EliminarReservaCommand { get; }

        public ReservasViewModel()
        {
            Reservas = new ObservableCollection<Reserva>();

            CargarReservasCommand = new RelayCommand(CargarReservas);
            AgregarReservaCommand = new RelayCommand(AgregarReserva);
            ConfirmarReservaCommand = new RelayCommand(ConfirmarReserva, () => ReservaSeleccionada != null && ReservaSeleccionada.Estado == "Pendiente");
            CancelarReservaCommand = new RelayCommand(CancelarReserva, () => ReservaSeleccionada != null && ReservaSeleccionada.Estado != "Cancelada");
            EliminarReservaCommand = new RelayCommand(EliminarReserva, () => ReservaSeleccionada != null);

            CargarReservas();
        }

        private void CargarReservas()
        {
            Reservas.Clear();
            using var db = new TallerDbContext();

            var reservas = db.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Vehiculo)
                .OrderBy(r => r.FechaHoraCita)
                .ToList();

            foreach (var reserva in reservas)
            {
                Reservas.Add(reserva);
            }
        }

        private void AplicarFiltro(string filtro)
        {
            Reservas.Clear();
            using var db = new TallerDbContext();

            var query = db.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Vehiculo)
                .AsQueryable();

            if (filtro == "Pendiente")
            {
                query = query.Where(r => r.Estado == "Pendiente");
            }
            else if (filtro == "Confirmada")
            {
                query = query.Where(r => r.Estado == "Confirmada");
            }
            else if (filtro == "Hoy")
            {
                var hoy = DateTime.Today;
                var manana = hoy.AddDays(1);
                query = query.Where(r => r.FechaHoraCita >= hoy && r.FechaHoraCita < manana);
            }

            var reservas = query.OrderBy(r => r.FechaHoraCita).ToList();

            foreach (var reserva in reservas)
            {
                Reservas.Add(reserva);
            }
        }

        private void AgregarReserva()
        {
            using var db = new TallerDbContext();

            var primerCliente = db.Clientes.FirstOrDefault();

            if (primerCliente == null)
            {
                System.Windows.MessageBox.Show(
                    "Debe registrar al menos un cliente antes de crear reservas.",
                    "Sin Clientes",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var primerVehiculo = db.Vehiculos
                .Where(v => v.ClienteID == primerCliente.ClienteID)
                .FirstOrDefault();

            var nueva = new Reserva
            {
                ClienteID = primerCliente.ClienteID,
                VehiculoID = primerVehiculo?.VehiculoID,
                FechaReserva = DateTime.Now,
                FechaHoraCita = DateTime.Now.AddDays(1).Date.AddHours(9), // Mañana a las 9 AM
                TipoServicio = "Mecánica",
                Observaciones = "Reserva nueva",
                Estado = "Pendiente"
            };

            db.Reservas.Add(nueva);
            db.SaveChanges();

            var reservaConRelaciones = db.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Vehiculo)
                .FirstOrDefault(r => r.ReservaID == nueva.ReservaID);

            if (reservaConRelaciones != null)
            {
                Reservas.Add(reservaConRelaciones);
            }

            System.Windows.MessageBox.Show(
                "Reserva creada exitosamente.",
                "Éxito",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        private void ConfirmarReserva()
        {
            if (ReservaSeleccionada == null || ReservaSeleccionada.Estado != "Pendiente")
                return;

            var resultado = System.Windows.MessageBox.Show(
                $"¿Confirmar la reserva para {ReservaSeleccionada.Cliente?.Nombre} {ReservaSeleccionada.Cliente?.Apellido}?",
                "Confirmar Reserva",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (resultado == System.Windows.MessageBoxResult.Yes)
            {
                using var db = new TallerDbContext();
                var reserva = db.Reservas.Find(ReservaSeleccionada.ReservaID);

                if (reserva != null)
                {
                    reserva.Estado = "Confirmada";
                    db.SaveChanges();

                    ReservaSeleccionada.Estado = "Confirmada";
                    OnPropertyChanged(nameof(Reservas));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private void CancelarReserva()
        {
            if (ReservaSeleccionada == null || ReservaSeleccionada.Estado == "Cancelada")
                return;

            var resultado = System.Windows.MessageBox.Show(
                $"¿Cancelar la reserva para {ReservaSeleccionada.Cliente?.Nombre} {ReservaSeleccionada.Cliente?.Apellido}?",
                "Cancelar Reserva",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (resultado == System.Windows.MessageBoxResult.Yes)
            {
                using var db = new TallerDbContext();
                var reserva = db.Reservas.Find(ReservaSeleccionada.ReservaID);

                if (reserva != null)
                {
                    reserva.Estado = "Cancelada";
                    db.SaveChanges();

                    ReservaSeleccionada.Estado = "Cancelada";
                    OnPropertyChanged(nameof(Reservas));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private void EliminarReserva()
        {
            if (ReservaSeleccionada == null) return;

            var resultado = System.Windows.MessageBox.Show(
                $"¿Está seguro de eliminar la reserva #{ReservaSeleccionada.ReservaID}?",
                "Confirmar Eliminación",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (resultado == System.Windows.MessageBoxResult.Yes)
            {
                using var db = new TallerDbContext();
                var reserva = db.Reservas.Find(ReservaSeleccionada.ReservaID);

                if (reserva != null)
                {
                    db.Reservas.Remove(reserva);
                    db.SaveChanges();
                    Reservas.Remove(ReservaSeleccionada);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
