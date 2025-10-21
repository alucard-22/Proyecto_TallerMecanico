using Proyecto_taller.Data;
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
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.ViewModels
{
    public class ReportesViewModel : INotifyPropertyChanged
    {
        private DateTime _fechaInicio;
        private DateTime _fechaFin;

        // Estadísticas
        private decimal _ingresosTotales;
        private int _totalFacturas;
        private int _trabajosRealizados;
        private int _trabajosEnProceso;
        private int _clientesAtendidos;
        private int _clientesNuevos;
        private int _repuestosVendidos;
        private decimal _valorRepuestos;
        private int _trabajosPendientes;
        private int _trabajosFinalizados;

        // Anchos de barras para gráfico
        private double _barraPendientes;
        private double _barraEnProceso;
        private double _barraFinalizados;

        public ObservableCollection<ServicioReporte> TopServicios { get; set; }

        // Propiedades de Fecha
        public DateTime FechaInicio
        {
            get => _fechaInicio;
            set { _fechaInicio = value; OnPropertyChanged(); }
        }

        public DateTime FechaFin
        {
            get => _fechaFin;
            set { _fechaFin = value; OnPropertyChanged(); }
        }

        // Propiedades de Estadísticas
        public decimal IngresosTotales
        {
            get => _ingresosTotales;
            set { _ingresosTotales = value; OnPropertyChanged(); }
        }

        public int TotalFacturas
        {
            get => _totalFacturas;
            set { _totalFacturas = value; OnPropertyChanged(); }
        }

        public int TrabajosRealizados
        {
            get => _trabajosRealizados;
            set { _trabajosRealizados = value; OnPropertyChanged(); }
        }

        public int TrabajosEnProceso
        {
            get => _trabajosEnProceso;
            set { _trabajosEnProceso = value; OnPropertyChanged(); }
        }

        public int ClientesAtendidos
        {
            get => _clientesAtendidos;
            set { _clientesAtendidos = value; OnPropertyChanged(); }
        }

        public int ClientesNuevos
        {
            get => _clientesNuevos;
            set { _clientesNuevos = value; OnPropertyChanged(); }
        }

        public int RepuestosVendidos
        {
            get => _repuestosVendidos;
            set { _repuestosVendidos = value; OnPropertyChanged(); }
        }

        public decimal ValorRepuestos
        {
            get => _valorRepuestos;
            set { _valorRepuestos = value; OnPropertyChanged(); }
        }

        public int TrabajosPendientes
        {
            get => _trabajosPendientes;
            set { _trabajosPendientes = value; OnPropertyChanged(); }
        }

        public int TrabajosFinalizados
        {
            get => _trabajosFinalizados;
            set { _trabajosFinalizados = value; OnPropertyChanged(); }
        }

        // Propiedades para gráfico de barras
        public double BarraPendientes
        {
            get => _barraPendientes;
            set { _barraPendientes = value; OnPropertyChanged(); }
        }

        public double BarraEnProceso
        {
            get => _barraEnProceso;
            set { _barraEnProceso = value; OnPropertyChanged(); }
        }

        public double BarraFinalizados
        {
            get => _barraFinalizados;
            set { _barraFinalizados = value; OnPropertyChanged(); }
        }

        // Comandos
        public ICommand ActualizarReportesCommand { get; }
        public ICommand ExportarExcelCommand { get; }
        public ICommand AplicarFiltroCommand { get; }
        public ICommand FiltroHoyCommand { get; }
        public ICommand FiltroSemanaCommand { get; }
        public ICommand FiltroMesCommand { get; }
        public ICommand FiltroAnioCommand { get; }

        public ReportesViewModel()
        {
            TopServicios = new ObservableCollection<ServicioReporte>();

            // Inicializar fechas (último mes)
            FechaFin = DateTime.Now;
            FechaInicio = DateTime.Now.AddMonths(-1);

            ActualizarReportesCommand = new RelayCommand(CargarReportes);
            ExportarExcelCommand = new RelayCommand(ExportarExcel);
            AplicarFiltroCommand = new RelayCommand(CargarReportes);
            FiltroHoyCommand = new RelayCommand(FiltroHoy);
            FiltroSemanaCommand = new RelayCommand(FiltroSemana);
            FiltroMesCommand = new RelayCommand(FiltroMes);
            FiltroAnioCommand = new RelayCommand(FiltroAnio);

            CargarReportes();
        }

        private void FiltroHoy()
        {
            FechaInicio = DateTime.Today;
            FechaFin = DateTime.Today.AddDays(1).AddSeconds(-1);
            CargarReportes();
        }

        private void FiltroSemana()
        {
            var hoy = DateTime.Today;
            int diasHastaLunes = ((int)hoy.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            FechaInicio = hoy.AddDays(-diasHastaLunes);
            FechaFin = FechaInicio.AddDays(7).AddSeconds(-1);
            CargarReportes();
        }

        private void FiltroMes()
        {
            FechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            FechaFin = FechaInicio.AddMonths(1).AddSeconds(-1);
            CargarReportes();
        }

        private void FiltroAnio()
        {
            FechaInicio = new DateTime(DateTime.Now.Year, 1, 1);
            FechaFin = new DateTime(DateTime.Now.Year, 12, 31, 23, 59, 59);
            CargarReportes();
        }

        private void CargarReportes()
        {
            using var db = new TallerDbContext();

            // Ingresos Totales
            IngresosTotales = db.Facturas
                .Where(f => f.Estado == "Pagada" &&
                           f.FechaEmision >= FechaInicio &&
                           f.FechaEmision <= FechaFin)
                .Sum(f => (decimal?)f.Total) ?? 0;

            TotalFacturas = db.Facturas
                .Count(f => f.Estado == "Pagada" &&
                           f.FechaEmision >= FechaInicio &&
                           f.FechaEmision <= FechaFin);

            // Trabajos
            var trabajosDelPeriodo = db.Trabajos
                .Where(t => t.FechaIngreso >= FechaInicio && t.FechaIngreso <= FechaFin)
                .ToList();

            TrabajosRealizados = trabajosDelPeriodo.Count;
            TrabajosPendientes = trabajosDelPeriodo.Count(t => t.Estado == "Pendiente");
            TrabajosEnProceso = trabajosDelPeriodo.Count(t => t.Estado == "En Progreso");
            TrabajosFinalizados = trabajosDelPeriodo.Count(t => t.Estado == "Finalizado");

            // Calcular anchos de barras (proporcional, máximo 300px)
            int maxTrabajos = TrabajosRealizados > 0 ? TrabajosRealizados : 1;
            BarraPendientes = (TrabajosPendientes / (double)maxTrabajos) * 300;
            BarraEnProceso = (TrabajosEnProceso / (double)maxTrabajos) * 300;
            BarraFinalizados = (TrabajosFinalizados / (double)maxTrabajos) * 300;

            // Clientes
            var vehiculosAtendidos = db.Trabajos
                .Include(t => t.Vehiculo)
                .Where(t => t.FechaIngreso >= FechaInicio && t.FechaIngreso <= FechaFin)
                .Select(t => t.Vehiculo.ClienteID)
                .Distinct()
                .ToList();

            ClientesAtendidos = vehiculosAtendidos.Count;

            ClientesNuevos = db.Clientes
                .Count(c => c.FechaRegistro >= FechaInicio && c.FechaRegistro <= FechaFin);

            // Repuestos
            var repuestosUsados = db.Trabajos_Repuestos
                .Include(tr => tr.Trabajo)
                .Where(tr => tr.Trabajo.FechaIngreso >= FechaInicio &&
                            tr.Trabajo.FechaIngreso <= FechaFin)
                .ToList();

            RepuestosVendidos = repuestosUsados.Sum(tr => tr.Cantidad);
            ValorRepuestos = repuestosUsados.Sum(tr => tr.Subtotal);

            // Top Servicios
            CargarTopServicios();
        }

        private void CargarTopServicios()
        {
            using var db = new TallerDbContext();

            TopServicios.Clear();

            var servicios = db.Trabajos_Servicios
                .Include(ts => ts.Servicio)
                .Include(ts => ts.Trabajo)
                .Where(ts => ts.Trabajo.FechaIngreso >= FechaInicio &&
                            ts.Trabajo.FechaIngreso <= FechaFin)
                .GroupBy(ts => new { ts.ServicioID, ts.Servicio.Nombre })
                .Select(g => new
                {
                    Nombre = g.Key.Nombre,
                    Cantidad = g.Sum(ts => ts.Cantidad),
                    Total = g.Sum(ts => ts.Subtotal)
                })
                .OrderByDescending(s => s.Cantidad)
                .Take(10)
                .ToList();

            foreach (var servicio in servicios)
            {
                TopServicios.Add(new ServicioReporte
                {
                    Nombre = servicio.Nombre,
                    Cantidad = servicio.Cantidad,
                    Total = servicio.Total
                });
            }

            if (TopServicios.Count == 0)
            {
                TopServicios.Add(new ServicioReporte
                {
                    Nombre = "Sin datos para el período seleccionado",
                    Cantidad = 0,
                    Total = 0
                });
            }
        }

        private void ExportarExcel()
        {
            var reporte = $"REPORTE DEL TALLER MECÁNICO\n" +
                         $"Período: {FechaInicio:dd/MM/yyyy} - {FechaFin:dd/MM/yyyy}\n" +
                         $"{'=',60}\n\n" +
                         $"RESUMEN FINANCIERO\n" +
                         $"{'-',40}\n" +
                         $"Ingresos Totales:        Bs. {IngresosTotales:N2}\n" +
                         $"Total Facturas:          {TotalFacturas}\n" +
                         $"Promedio por Factura:    Bs. {(TotalFacturas > 0 ? IngresosTotales / TotalFacturas : 0):N2}\n\n" +
                         $"TRABAJOS REALIZADOS\n" +
                         $"{'-',40}\n" +
                         $"Total Trabajos:          {TrabajosRealizados}\n" +
                         $"  - Pendientes:          {TrabajosPendientes}\n" +
                         $"  - En Progreso:         {TrabajosEnProceso}\n" +
                         $"  - Finalizados:         {TrabajosFinalizados}\n\n" +
                         $"CLIENTES\n" +
                         $"{'-',40}\n" +
                         $"Clientes Atendidos:      {ClientesAtendidos}\n" +
                         $"Clientes Nuevos:         {ClientesNuevos}\n\n" +
                         $"REPUESTOS\n" +
                         $"{'-',40}\n" +
                         $"Repuestos Vendidos:      {RepuestosVendidos} unidades\n" +
                         $"Valor Total Repuestos:   Bs. {ValorRepuestos:N2}\n\n" +
                         $"SERVICIOS MÁS SOLICITADOS\n" +
                         $"{'-',40}\n";

            foreach (var servicio in TopServicios)
            {
                reporte += $"{servicio.Nombre,-30} {servicio.Cantidad,5} veces   Bs. {servicio.Total,10:N2}\n";
            }

            try
            {
                // Guardar en archivo de texto
                var fecha = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var nombreArchivo = $"Reporte_Taller_{fecha}.txt";
                var rutaEscritorio = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var rutaCompleta = System.IO.Path.Combine(rutaEscritorio, nombreArchivo);

                System.IO.File.WriteAllText(rutaCompleta, reporte);

                MessageBox.Show(
                    $"Reporte exportado exitosamente:\n\n{rutaCompleta}",
                    "Exportación Exitosa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al exportar el reporte:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Clase auxiliar para el reporte de servicios
    public class ServicioReporte
    {
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
    }
}
