using Proyecto_taller.Data;
using Proyecto_taller.Models;
using Proyecto_taller.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public InicioViewModel()
        {
            ActividadReciente = new ObservableCollection<string>();
            TrabajosPendientes = new ObservableCollection<Trabajo>();
            RepuestosStockBajo = new ObservableCollection<Repuesto>();

            AbrirRegistroRapidoCommand = new RelayCommand(AbrirRegistroRapido);

            CargarEstadisticas();
            CargarActividadReciente();
            CargarTrabajosPendientes();
            CargarRepuestosStockBajo();
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

        private void AbrirRegistroRapido()
        {
            var ventana = new RegistroRapidoWindow();

            if (ventana.ShowDialog() == true)
            {
                // Actualizar las estadísticas después de registrar el trabajo
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