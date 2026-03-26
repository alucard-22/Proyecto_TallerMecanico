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
        // ── Período ───────────────────────────────────────────────────────────
        private DateTime _fechaInicio;
        private DateTime _fechaFin;

        // ── Financiero ────────────────────────────────────────────────────────
        private decimal _ingresosTotales;
        private int _totalFacturas;
        private decimal _ticketPromedio;

        // ── Trabajos ──────────────────────────────────────────────────────────
        private int _trabajosRealizados;
        private int _trabajosPendientes;
        private int _trabajosEnProceso;
        private int _trabajosFinalizados;

        // ── Clientes ──────────────────────────────────────────────────────────
        private int _clientesAtendidos;
        private int _clientesNuevos;

        // ── Repuestos ─────────────────────────────────────────────────────────
        private int _repuestosVendidos;
        private decimal _valorRepuestos;

        // ── Reservas ──────────────────────────────────────────────────────────
        private int _totalReservas;
        private int _reservasCompletadas;
        private int _reservasCanceladas;
        private decimal _tasaAsistencia;

        // ── UI state ──────────────────────────────────────────────────────────
        private bool _cargando;
        private string _mensajeCarga = string.Empty;

        // ── Barras visuales (ancho en px, máx 300) ────────────────────────────
        private double _barraPendientes;
        private double _barraEnProceso;
        private double _barraFinalizados;

        // ── Colecciones ───────────────────────────────────────────────────────
        public ObservableCollection<ServicioReporte> TopServicios { get; } = new();
        public ObservableCollection<ClienteReporte> TopClientes { get; } = new();
        public ObservableCollection<IngresoMes> IngresosPorMes { get; } = new();

        // ─────────────────────────────────────────────────────────────────────
        //  PROPIEDADES PÚBLICAS
        // ─────────────────────────────────────────────────────────────────────

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

        // Financiero
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
        public decimal TicketPromedio
        {
            get => _ticketPromedio;
            set { _ticketPromedio = value; OnPropertyChanged(); }
        }

        // Trabajos
        public int TrabajosRealizados
        {
            get => _trabajosRealizados;
            set { _trabajosRealizados = value; OnPropertyChanged(); }
        }
        public int TrabajosPendientes
        {
            get => _trabajosPendientes;
            set { _trabajosPendientes = value; OnPropertyChanged(); }
        }
        public int TrabajosEnProceso
        {
            get => _trabajosEnProceso;
            set { _trabajosEnProceso = value; OnPropertyChanged(); }
        }
        public int TrabajosFinalizados
        {
            get => _trabajosFinalizados;
            set { _trabajosFinalizados = value; OnPropertyChanged(); }
        }

        // Clientes
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

        // Repuestos
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

        // Reservas
        public int TotalReservas
        {
            get => _totalReservas;
            set { _totalReservas = value; OnPropertyChanged(); }
        }
        public int ReservasCompletadas
        {
            get => _reservasCompletadas;
            set { _reservasCompletadas = value; OnPropertyChanged(); }
        }
        public int ReservasCanceladas
        {
            get => _reservasCanceladas;
            set { _reservasCanceladas = value; OnPropertyChanged(); }
        }
        public decimal TasaAsistencia
        {
            get => _tasaAsistencia;
            set
            {
                _tasaAsistencia = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AnchoBarra));
            }
        }

        /// <summary>Ancho en px de la barra de tasa de asistencia (max 160px = 100%)</summary>
        public double AnchoBarra =>
            Math.Max(3, (double)Math.Min(100, Math.Max(0, _tasaAsistencia)) / 100.0 * 160);

        // UI state
        public bool Cargando
        {
            get => _cargando;
            set { _cargando = value; OnPropertyChanged(); }
        }
        public string MensajeCarga
        {
            get => _mensajeCarga;
            set { _mensajeCarga = value; OnPropertyChanged(); }
        }

        // Barras
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

        // ─────────────────────────────────────────────────────────────────────
        //  COMANDOS
        // ─────────────────────────────────────────────────────────────────────

        public ICommand ActualizarReportesCommand { get; }
        public ICommand ExportarCsvCommand { get; }
        public ICommand AplicarFiltroCommand { get; }
        public ICommand FiltroHoyCommand { get; }
        public ICommand FiltroSemanaCommand { get; }
        public ICommand FiltroMesCommand { get; }
        public ICommand FiltroAnioCommand { get; }

        // ─────────────────────────────────────────────────────────────────────
        //  CONSTRUCTOR
        // ─────────────────────────────────────────────────────────────────────

        public ReportesViewModel()
        {
            // Período por defecto: mes actual
            FechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            FechaFin = DateTime.Now;

            ActualizarReportesCommand = new RelayCommand(async () => await CargarReportesAsync());
            ExportarCsvCommand = new RelayCommand(ExportarCsv, () => !Cargando);
            AplicarFiltroCommand = new RelayCommand(async () => await CargarReportesAsync());
            FiltroHoyCommand = new RelayCommand(async () => { SetFiltroHoy(); await CargarReportesAsync(); });
            FiltroSemanaCommand = new RelayCommand(async () => { SetFiltroSemana(); await CargarReportesAsync(); });
            FiltroMesCommand = new RelayCommand(async () => { SetFiltroMes(); await CargarReportesAsync(); });
            FiltroAnioCommand = new RelayCommand(async () => { SetFiltroAnio(); await CargarReportesAsync(); });

            // Carga inicial sin bloquear el constructor
            _ = CargarReportesAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  FILTROS DE PERÍODO
        //  FIX: se usa FechaFin exclusiva (< FechaFin) en lugar del patrón
        //       frágil AddSeconds(-1) que podía excluir registros de medianoche.
        // ─────────────────────────────────────────────────────────────────────

        private void SetFiltroHoy()
        {
            FechaInicio = DateTime.Today;
            FechaFin = DateTime.Today.AddDays(1);   // exclusivo
        }

        private void SetFiltroSemana()
        {
            var hoy = DateTime.Today;
            int diasHastaLunes = ((int)hoy.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            FechaInicio = hoy.AddDays(-diasHastaLunes);
            FechaFin = FechaInicio.AddDays(7);      // exclusivo
        }

        private void SetFiltroMes()
        {
            FechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            FechaFin = FechaInicio.AddMonths(1);    // exclusivo
        }

        private void SetFiltroAnio()
        {
            FechaInicio = new DateTime(DateTime.Now.Year, 1, 1);
            FechaFin = new DateTime(DateTime.Now.Year + 1, 1, 1); // exclusivo
        }

        // ─────────────────────────────────────────────────────────────────────
        //  CARGA PRINCIPAL (async para no bloquear el hilo UI)
        // ─────────────────────────────────────────────────────────────────────

        private async Task CargarReportesAsync()
        {
            Cargando = true;
            MensajeCarga = "Cargando datos...";

            try
            {
                // Todas las queries en un único Task.Run para no bloquear el dispatcher
                var datos = await Task.Run(() => ObtenerDatos(FechaInicio, FechaFin));

                // Actualizar propiedades en el hilo UI
                AplicarDatos(datos);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar reportes:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cargando = false;
                MensajeCarga = string.Empty;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  OBTENER DATOS (se ejecuta en hilo de fondo)
        // ─────────────────────────────────────────────────────────────────────

        private ReportesDatos ObtenerDatos(DateTime desde, DateTime hasta)
        {
            using var db = new TallerDbContext();
            var d = new ReportesDatos();

            // ── Financiero ────────────────────────────────────────────────────
            var facturas = db.Facturas
                .Where(f => f.Estado == "Pagada"
                         && f.FechaEmision >= desde
                         && f.FechaEmision < hasta)
                .ToList();

            d.IngresosTotales = facturas.Sum(f => f.Total);
            d.TotalFacturas = facturas.Count;
            d.TicketPromedio = d.TotalFacturas > 0
                ? Math.Round(d.IngresosTotales / d.TotalFacturas, 2)
                : 0;

            // ── Trabajos ──────────────────────────────────────────────────────
            var trabajos = db.Trabajos
                .Where(t => t.FechaIngreso >= desde && t.FechaIngreso < hasta)
                .ToList();

            d.TrabajosRealizados = trabajos.Count;
            d.TrabajosPendientes = trabajos.Count(t => t.Estado == "Pendiente");
            d.TrabajosEnProceso = trabajos.Count(t => t.Estado == "En Progreso");
            d.TrabajosFinalizados = trabajos.Count(t => t.Estado == "Finalizado");

            // ── Clientes ──────────────────────────────────────────────────────
            d.ClientesAtendidos = db.Trabajos
                .Include(t => t.Vehiculo)
                .Where(t => t.FechaIngreso >= desde && t.FechaIngreso < hasta)
                .Select(t => t.Vehiculo.ClienteID)
                .Distinct()
                .Count();

            d.ClientesNuevos = db.Clientes
                .Count(c => c.FechaRegistro >= desde && c.FechaRegistro < hasta);

            // ── Repuestos ─────────────────────────────────────────────────────
            var repuestosUsados = db.Trabajos_Repuestos
                .Include(tr => tr.Trabajo)
                .Where(tr => tr.Trabajo.FechaIngreso >= desde
                          && tr.Trabajo.FechaIngreso < hasta)
                .ToList();

            d.RepuestosVendidos = repuestosUsados.Sum(tr => tr.Cantidad);
            d.ValorRepuestos = repuestosUsados.Sum(tr => tr.Subtotal);

            // ── Reservas ──────────────────────────────────────────────────────
            var reservas = db.Reservas
                .Where(r => r.FechaHoraCita >= desde && r.FechaHoraCita < hasta)
                .ToList();

            d.TotalReservas = reservas.Count;
            d.ReservasCompletadas = reservas.Count(r => r.Estado == "Completada");
            d.ReservasCanceladas = reservas.Count(r =>
                r.Estado == "Cancelada" || r.Estado == "No Asistió");

            // Tasa de asistencia: completadas / (no canceladas) — evitar /0
            int reservasValidas = d.TotalReservas - d.ReservasCanceladas;
            d.TasaAsistencia = reservasValidas > 0
                ? Math.Round((decimal)d.ReservasCompletadas / reservasValidas * 100, 1)
                : 0;

            // ── Top servicios ─────────────────────────────────────────────────
            d.TopServicios = db.Trabajos_Servicios
                .Include(ts => ts.Servicio)
                .Include(ts => ts.Trabajo)
                .Where(ts => ts.Trabajo.FechaIngreso >= desde
                          && ts.Trabajo.FechaIngreso < hasta)
                .GroupBy(ts => new { ts.ServicioID, ts.Servicio.Nombre })
                .Select(g => new ServicioReporte
                {
                    Nombre = g.Key.Nombre,
                    Cantidad = g.Sum(ts => ts.Cantidad),
                    Total = g.Sum(ts => ts.Subtotal)
                })
                .OrderByDescending(s => s.Cantidad)
                .Take(10)
                .ToList();

            // ── Top 5 clientes ────────────────────────────────────────────────
            d.TopClientes = db.Trabajos
                .Include(t => t.Vehiculo).ThenInclude(v => v.Cliente)
                .Where(t => t.FechaIngreso >= desde && t.FechaIngreso < hasta)
                .GroupBy(t => new
                {
                    t.Vehiculo.ClienteID,
                    t.Vehiculo.Cliente.Nombre,
                    t.Vehiculo.Cliente.Apellido,
                    t.Vehiculo.Cliente.Telefono
                })
                .Select(g => new ClienteReporte
                {
                    Nombre = $"{g.Key.Nombre} {g.Key.Apellido}",
                    Telefono = g.Key.Telefono,
                    TotalTrabajos = g.Count(),
                    TotalGastado = g.Sum(t => t.PrecioFinal ?? 0)
                })
                .OrderByDescending(c => c.TotalTrabajos)
                .Take(5)
                .ToList();

            // ── Ingresos por mes (últimos 12 meses, ignora el filtro de período)
            var hace12meses = DateTime.Today.AddMonths(-11);
            var inicioAnio = new DateTime(hace12meses.Year, hace12meses.Month, 1);

            d.IngresosPorMes = db.Facturas
                .Where(f => f.Estado == "Pagada" && f.FechaEmision >= inicioAnio)
                .GroupBy(f => new { f.FechaEmision.Year, f.FechaEmision.Month })
                .Select(g => new IngresoMes
                {
                    Anio = g.Key.Year,
                    Mes = g.Key.Month,
                    Ingreso = g.Sum(f => f.Total)
                })
                .OrderBy(x => x.Anio).ThenBy(x => x.Mes)
                .ToList();

            // Rellenar meses sin datos con 0
            var mesesCompletos = new List<IngresoMes>();
            for (int i = 0; i < 12; i++)
            {
                var fecha = inicioAnio.AddMonths(i);
                var existente = d.IngresosPorMes
                    .FirstOrDefault(x => x.Anio == fecha.Year && x.Mes == fecha.Month);
                mesesCompletos.Add(existente ?? new IngresoMes
                {
                    Anio = fecha.Year,
                    Mes = fecha.Month,
                    Ingreso = 0
                });
            }
            d.IngresosPorMes = mesesCompletos;

            return d;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  APLICAR DATOS AL VIEWMODEL (hilo UI)
        // ─────────────────────────────────────────────────────────────────────

        private void AplicarDatos(ReportesDatos d)
        {
            // Financiero
            IngresosTotales = d.IngresosTotales;
            TotalFacturas = d.TotalFacturas;
            TicketPromedio = d.TicketPromedio;

            // Trabajos
            TrabajosRealizados = d.TrabajosRealizados;
            TrabajosPendientes = d.TrabajosPendientes;
            TrabajosEnProceso = d.TrabajosEnProceso;
            TrabajosFinalizados = d.TrabajosFinalizados;

            // Barras visuales
            // FIX: evitar NaN y barras de 0px invisibles.
            // Mínimo 2px para que siempre haya indicación visual.
            double maxTrabajos = Math.Max(d.TrabajosRealizados, 1);
            BarraPendientes = Math.Max(2, (d.TrabajosPendientes / maxTrabajos) * 300);
            BarraEnProceso = Math.Max(2, (d.TrabajosEnProceso / maxTrabajos) * 300);
            BarraFinalizados = Math.Max(2, (d.TrabajosFinalizados / maxTrabajos) * 300);

            // Si realmente son 0 trabajos, forzar barra a 0 (no a 2)
            if (d.TrabajosRealizados == 0)
            {
                BarraPendientes = BarraEnProceso = BarraFinalizados = 0;
            }

            // Clientes
            ClientesAtendidos = d.ClientesAtendidos;
            ClientesNuevos = d.ClientesNuevos;

            // Repuestos
            RepuestosVendidos = d.RepuestosVendidos;
            ValorRepuestos = d.ValorRepuestos;

            // Reservas
            TotalReservas = d.TotalReservas;
            ReservasCompletadas = d.ReservasCompletadas;
            ReservasCanceladas = d.ReservasCanceladas;
            TasaAsistencia = d.TasaAsistencia;

            // Top servicios
            TopServicios.Clear();
            if (d.TopServicios.Count == 0)
                TopServicios.Add(new ServicioReporte
                { Nombre = "Sin datos para el período", Cantidad = 0, Total = 0 });
            else
                foreach (var s in d.TopServicios) TopServicios.Add(s);

            // Top clientes
            TopClientes.Clear();
            if (d.TopClientes.Count == 0)
                TopClientes.Add(new ClienteReporte
                { Nombre = "Sin datos para el período", TotalTrabajos = 0, TotalGastado = 0 });
            else
                foreach (var c in d.TopClientes) TopClientes.Add(c);

            // Ingresos por mes (para el gráfico de tendencia)
            IngresosPorMes.Clear();
            double maxIngreso = d.IngresosPorMes.Count > 0
                ? (double)d.IngresosPorMes.Max(m => m.Ingreso)
                : 1;
            if (maxIngreso <= 0) maxIngreso = 1;

            foreach (var m in d.IngresosPorMes)
            {
                m.AlturaBarraPx = Math.Max(3, (double)m.Ingreso / maxIngreso * 160);
                if (m.Ingreso == 0) m.AlturaBarraPx = 3; // mínimo visual
                IngresosPorMes.Add(m);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  EXPORTAR A CSV
        //  FIX: antes generaba un .txt con formato de texto plano.
        //  Ahora genera un .csv real, separado por punto y coma (compatible con
        //  Excel en español que usa ; como separador regional).
        // ─────────────────────────────────────────────────────────────────────

        private void ExportarCsv()
        {
            try
            {
                var sb = new StringBuilder();
                string sep = ";";   // separador compatible con Excel en español

                // ── Hoja 1: Resumen general ───────────────────────────────────
                sb.AppendLine("REPORTE DEL TALLER MECÁNICO");
                sb.AppendLine($"Período{sep}{FechaInicio:dd/MM/yyyy}{sep}{FechaFin.AddDays(-1):dd/MM/yyyy}");
                sb.AppendLine($"Generado{sep}{DateTime.Now:dd/MM/yyyy HH:mm}");
                sb.AppendLine();

                sb.AppendLine("RESUMEN FINANCIERO");
                sb.AppendLine($"Concepto{sep}Valor");
                sb.AppendLine($"Ingresos totales{sep}{IngresosTotales:N2}");
                sb.AppendLine($"Total facturas{sep}{TotalFacturas}");
                sb.AppendLine($"Ticket promedio{sep}{TicketPromedio:N2}");
                sb.AppendLine();

                sb.AppendLine("TRABAJOS");
                sb.AppendLine($"Concepto{sep}Cantidad");
                sb.AppendLine($"Total trabajos{sep}{TrabajosRealizados}");
                sb.AppendLine($"Pendientes{sep}{TrabajosPendientes}");
                sb.AppendLine($"En Progreso{sep}{TrabajosEnProceso}");
                sb.AppendLine($"Finalizados{sep}{TrabajosFinalizados}");
                sb.AppendLine();

                sb.AppendLine("CLIENTES");
                sb.AppendLine($"Concepto{sep}Valor");
                sb.AppendLine($"Clientes atendidos{sep}{ClientesAtendidos}");
                sb.AppendLine($"Clientes nuevos{sep}{ClientesNuevos}");
                sb.AppendLine();

                sb.AppendLine("REPUESTOS");
                sb.AppendLine($"Concepto{sep}Valor");
                sb.AppendLine($"Unidades vendidas{sep}{RepuestosVendidos}");
                sb.AppendLine($"Valor total{sep}{ValorRepuestos:N2}");
                sb.AppendLine();

                sb.AppendLine("RESERVAS");
                sb.AppendLine($"Concepto{sep}Valor");
                sb.AppendLine($"Total reservas{sep}{TotalReservas}");
                sb.AppendLine($"Completadas{sep}{ReservasCompletadas}");
                sb.AppendLine($"Canceladas / No asistió{sep}{ReservasCanceladas}");
                sb.AppendLine($"Tasa de asistencia{sep}{TasaAsistencia}%");
                sb.AppendLine();

                // ── Servicios más solicitados ─────────────────────────────────
                sb.AppendLine("SERVICIOS MÁS SOLICITADOS");
                sb.AppendLine($"Servicio{sep}Cantidad{sep}Total (Bs.)");
                foreach (var s in TopServicios)
                    sb.AppendLine($"{EscaparCsv(s.Nombre, sep)}{sep}{s.Cantidad}{sep}{s.Total:N2}");
                sb.AppendLine();

                // ── Top clientes ──────────────────────────────────────────────
                sb.AppendLine("TOP 5 CLIENTES");
                sb.AppendLine($"Cliente{sep}Teléfono{sep}Trabajos{sep}Total gastado (Bs.)");
                foreach (var c in TopClientes)
                    sb.AppendLine(
                        $"{EscaparCsv(c.Nombre, sep)}{sep}" +
                        $"{EscaparCsv(c.Telefono ?? "", sep)}{sep}" +
                        $"{c.TotalTrabajos}{sep}" +
                        $"{c.TotalGastado:N2}");
                sb.AppendLine();

                // ── Ingresos por mes ──────────────────────────────────────────
                sb.AppendLine("INGRESOS POR MES (últimos 12 meses)");
                sb.AppendLine($"Mes{sep}Año{sep}Ingresos (Bs.)");
                foreach (var m in IngresosPorMes)
                    sb.AppendLine(
                        $"{m.NombreMes}{sep}{m.Anio}{sep}{m.Ingreso:N2}");

                // ── Guardar en escritorio ─────────────────────────────────────
                var escritorio = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var nombreArchivo = $"Reporte_Taller_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var rutaCompleta = Path.Combine(escritorio, nombreArchivo);

                // UTF-8 con BOM para que Excel lo abra directamente sin asistente
                File.WriteAllText(rutaCompleta, sb.ToString(),
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                var abrir = MessageBox.Show(
                    $"✅  REPORTE EXPORTADO CORRECTAMENTE\n\n" +
                    $"📄  {nombreArchivo}\n" +
                    $"📁  {escritorio}\n\n" +
                    $"El archivo .csv se puede abrir directamente con Excel.\n\n" +
                    $"¿Abrir el archivo ahora?",
                    "Exportación Exitosa",
                    MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (abrir == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(rutaCompleta)
                        { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar el reporte:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Escapa campos CSV que contengan el separador o comillas
        private static string EscaparCsv(string valor, string sep)
        {
            if (valor.Contains(sep) || valor.Contains('"') || valor.Contains('\n'))
                return $"\"{valor.Replace("\"", "\"\"")}\"";
            return valor;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  INotifyPropertyChanged
        // ─────────────────────────────────────────────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ─── DTOs internos (solo para el ViewModel) ───────────────────────────────

    internal class ReportesDatos
    {
        public decimal IngresosTotales { get; set; }
        public int TotalFacturas { get; set; }
        public decimal TicketPromedio { get; set; }
        public int TrabajosRealizados { get; set; }
        public int TrabajosPendientes { get; set; }
        public int TrabajosEnProceso { get; set; }
        public int TrabajosFinalizados { get; set; }
        public int ClientesAtendidos { get; set; }
        public int ClientesNuevos { get; set; }
        public int RepuestosVendidos { get; set; }
        public decimal ValorRepuestos { get; set; }
        public int TotalReservas { get; set; }
        public int ReservasCompletadas { get; set; }
        public int ReservasCanceladas { get; set; }
        public decimal TasaAsistencia { get; set; }
        public List<ServicioReporte> TopServicios { get; set; } = new();
        public List<ClienteReporte> TopClientes { get; set; } = new();
        public List<IngresoMes> IngresosPorMes { get; set; } = new();
    }

    // ─── Modelos públicos para binding en la vista ────────────────────────────

    public class ServicioReporte
    {
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
    }

    public class ClienteReporte
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public int TotalTrabajos { get; set; }
        public decimal TotalGastado { get; set; }
    }

    public class IngresoMes
    {
        public int Anio { get; set; }
        public int Mes { get; set; }
        public decimal Ingreso { get; set; }

        public string NombreMes => new DateTime(Anio, Mes, 1).ToString("MMM");
        public string Etiqueta => $"{NombreMes}\n{Anio}";

        // Año abreviado: solo aparece en enero para marcar el cambio de año en el gráfico
        public string AnioCorto => Mes == 1 ? Anio.ToString() : string.Empty;

        // Alto de la barra en px (máx 160). Se asigna en AplicarDatos() una vez
        // que se conoce el ingreso máximo de los 12 meses.
        public double AlturaBarraPx { get; set; }
    }
}
