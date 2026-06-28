using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using Proyecto_taller.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.ViewModels
{
    public class InicioViewModel : INotifyPropertyChanged
    {
        // Estadísticas
        private int _trabajosActivos;
        private int _totalClientes;
        private int _totalVehiculos;
        private decimal _ventasMes;

        public ObservableCollection<string> ActividadReciente { get; set; }
        public ObservableCollection<Trabajo> TrabajosPendientes { get; set; }
        public ObservableCollection<Repuesto> RepuestosStockBajo { get; set; }

        // ── NUEVO: reservas confirmadas/pendientes para el día siguiente ─────
        public ObservableCollection<Reserva> ReservasManana { get; set; }

        // Propiedades de Estadísticas
        public int TrabajosActivos
        {
            get => _trabajosActivos;
            set { _trabajosActivos = value; OnPropertyChanged(); }
        }

        public int TotalClientes
        {
            get => _totalClientes;
            set { _totalClientes = value; OnPropertyChanged(); }
        }

        public int TotalVehiculos
        {
            get => _totalVehiculos;
            set { _totalVehiculos = value; OnPropertyChanged(); }
        }

        public decimal VentasMes
        {
            get => _ventasMes;
            set { _ventasMes = value; OnPropertyChanged(); }
        }

        // Comando para abrir la ventana de registro rápido
        public ICommand AbrirRegistroRapidoCommand { get; }

        // ── NUEVO: comando para enviar recordatorio de una reserva específica ─
        public ICommand EnviarRecordatorioCommand { get; }

        public InicioViewModel()
        {
            ActividadReciente = new ObservableCollection<string>();
            TrabajosPendientes = new ObservableCollection<Trabajo>();
            RepuestosStockBajo = new ObservableCollection<Repuesto>();
            ReservasManana = new ObservableCollection<Reserva>();

            AbrirRegistroRapidoCommand = new RelayCommand(AbrirRegistroRapido);
            EnviarRecordatorioCommand = new RelayCommand<Reserva>(EnviarRecordatorio);

            CargarEstadisticas();
            CargarActividadReciente();
            CargarTrabajosPendientes();
            CargarRepuestosStockBajo();
            CargarReservasManana();
        }

        private void CargarEstadisticas()
        {
            using var db = new TallerDbContext();

            TrabajosActivos = db.Trabajos.Count(t => t.Estado == "Pendiente" || t.Estado == "En Progreso");
            TotalClientes = db.Clientes.Count();
            TotalVehiculos = db.Vehiculos.Count();

            var primerDiaMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            VentasMes = db.Facturas
                .Where(f => f.Estado == "Pagada" && f.FechaEmision >= primerDiaMes)
                .Sum(f => (decimal?)f.Total) ?? 0;
        }

        private void CargarActividadReciente()
        {
            using var db = new TallerDbContext();

            ActividadReciente.Clear();

            var ultimosTrabajos = db.Trabajos
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Cliente)
                .OrderByDescending(t => t.FechaIngreso)
                .Take(5)
                .ToList();

            foreach (var trabajo in ultimosTrabajos)
            {
                ActividadReciente.Add(
                    $"• Trabajo #{trabajo.TrabajoID} - {trabajo.Vehiculo?.Cliente?.Nombre} - " +
                    $"{trabajo.Vehiculo?.Marca} {trabajo.Vehiculo?.Modelo} ({trabajo.Estado})");
            }

            if (ActividadReciente.Count == 0)
            {
                ActividadReciente.Add("• No hay actividad reciente");
            }
        }

        private void CargarTrabajosPendientes()
        {
            using var db = new TallerDbContext();

            TrabajosPendientes.Clear();

            var pendientes = db.Trabajos
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Cliente)
                .Where(t => t.Estado == "Pendiente" || t.Estado == "En Progreso")
                .OrderBy(t => t.FechaIngreso)
                .Take(5)
                .ToList();

            foreach (var trabajo in pendientes)
            {
                TrabajosPendientes.Add(trabajo);
            }
        }

        private void CargarRepuestosStockBajo()
        {
            using var db = new TallerDbContext();

            RepuestosStockBajo.Clear();

            var stockBajo = db.Repuestos
                .Where(r => r.StockActual <= r.StockMinimo)
                .OrderBy(r => r.StockActual)
                .Take(5)
                .ToList();

            foreach (var repuesto in stockBajo)
            {
                RepuestosStockBajo.Add(repuesto);
            }

            if (RepuestosStockBajo.Count == 0)
            {
                var sinProblemas = new Repuesto
                {
                    Nombre = "✅ Todos los repuestos tienen stock adecuado",
                    StockActual = 0
                };
                RepuestosStockBajo.Add(sinProblemas);
            }
        }

        // ── NUEVO: cargar reservas confirmadas/pendientes de mañana ──────────
        // Solo se muestran reservas que aún requieren atención (Pendiente o
        // Confirmada) — las ya canceladas o completadas no tiene sentido
        // recordarlas. Ordenadas por hora para que el empleado pueda revisar
        // la agenda del día siguiente de un vistazo.
        private void CargarReservasManana()
        {
            using var db = new TallerDbContext();

            ReservasManana.Clear();

            var manana = DateTime.Today.AddDays(1);
            var finManana = manana.AddDays(1);

            var reservas = db.Reservas
                .Include(r => r.Vehiculo).ThenInclude(v => v.Cliente)
                .Where(r => r.FechaHoraCita >= manana && r.FechaHoraCita < finManana)
                .Where(r => r.Estado == "Pendiente" || r.Estado == "Confirmada")
                .OrderBy(r => r.FechaHoraCita)
                .ToList();

            foreach (var r in reservas)
                ReservasManana.Add(r);
        }

        // ── NUEVO: enviar recordatorio de WhatsApp desde el dashboard ────────
        private void EnviarRecordatorio(Reserva reserva)
        {
            if (reserva == null) return;

            var telefono = reserva.Vehiculo?.Cliente?.Telefono;
            if (string.IsNullOrWhiteSpace(telefono))
            {
                MessageBox.Show(
                    "Este cliente no tiene un número de teléfono registrado.",
                    "Sin teléfono", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            WhatsAppHelper.EnviarRecordatorio(
                telefono,
                $"{reserva.Vehiculo?.Cliente?.Nombre} {reserva.Vehiculo?.Cliente?.Apellido}",
                reserva.FechaHoraCita,
                reserva.TipoServicio);
        }

        private void AbrirRegistroRapido()
        {
            var ventana = new RegistroRapidoWindow();

            if (ventana.ShowDialog() == true)
            {
                CargarEstadisticas();
                CargarActividadReciente();
                CargarTrabajosPendientes();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
