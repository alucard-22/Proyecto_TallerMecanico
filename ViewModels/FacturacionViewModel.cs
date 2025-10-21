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
    public class FacturacionViewModel : INotifyPropertyChanged
    {
        private Factura _facturaSeleccionada;
        private bool _filtroTodas = true;
        private bool _filtroPagadas;
        private bool _filtroPendientes;
        private bool _filtroEsteMes;
        private decimal _totalFacturado;
        private int _totalFacturas;

        public ObservableCollection<Factura> Facturas { get; set; }

        public Factura FacturaSeleccionada
        {
            get => _facturaSeleccionada;
            set
            {
                _facturaSeleccionada = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public decimal TotalFacturado
        {
            get => _totalFacturado;
            set
            {
                _totalFacturado = value;
                OnPropertyChanged();
            }
        }

        public int TotalFacturas
        {
            get => _totalFacturas;
            set
            {
                _totalFacturas = value;
                OnPropertyChanged();
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

        public bool FiltroPagadas
        {
            get => _filtroPagadas;
            set
            {
                _filtroPagadas = value;
                OnPropertyChanged();
                if (value) AplicarFiltro("Pagada");
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

        public bool FiltroEsteMes
        {
            get => _filtroEsteMes;
            set
            {
                _filtroEsteMes = value;
                OnPropertyChanged();
                if (value) AplicarFiltro("EsteMes");
            }
        }

        public ICommand CargarFacturasCommand { get; }
        public ICommand NuevaFacturaCommand { get; }
        public ICommand VerDetalleCommand { get; }
        public ICommand ImprimirFacturaCommand { get; }
        public ICommand AnularFacturaCommand { get; }
        public ICommand EliminarFacturaCommand { get; }

        public FacturacionViewModel()
        {
            Facturas = new ObservableCollection<Factura>();

            CargarFacturasCommand = new RelayCommand(CargarFacturas);
            NuevaFacturaCommand = new RelayCommand(NuevaFactura);
            VerDetalleCommand = new RelayCommand(VerDetalle, () => FacturaSeleccionada != null);
            ImprimirFacturaCommand = new RelayCommand(ImprimirFactura, () => FacturaSeleccionada != null);
            AnularFacturaCommand = new RelayCommand(AnularFactura, () => FacturaSeleccionada != null && FacturaSeleccionada.Estado != "Anulada");
            EliminarFacturaCommand = new RelayCommand(EliminarFactura, () => FacturaSeleccionada != null);

            CargarFacturas();
        }

        private void CargarFacturas()
        {
            Facturas.Clear();
            using var db = new TallerDbContext();

            var facturas = db.Facturas
                .Include(f => f.Trabajo)
                    .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Cliente)
                .OrderByDescending(f => f.FechaEmision)
                .ToList();

            foreach (var factura in facturas)
            {
                Facturas.Add(factura);
            }

            ActualizarEstadisticas();
        }

        private void AplicarFiltro(string filtro)
        {
            Facturas.Clear();
            using var db = new TallerDbContext();

            var query = db.Facturas
                .Include(f => f.Trabajo)
                    .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Cliente)
                .AsQueryable();

            if (filtro == "Pagada")
            {
                query = query.Where(f => f.Estado == "Pagada");
            }
            else if (filtro == "Pendiente")
            {
                query = query.Where(f => f.Estado == "Pendiente");
            }
            else if (filtro == "EsteMes")
            {
                var primerDiaMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var ultimoDiaMes = primerDiaMes.AddMonths(1).AddDays(-1);
                query = query.Where(f => f.FechaEmision >= primerDiaMes && f.FechaEmision <= ultimoDiaMes);
            }

            var facturas = query.OrderByDescending(f => f.FechaEmision).ToList();

            foreach (var factura in facturas)
            {
                Facturas.Add(factura);
            }

            ActualizarEstadisticas();
        }

        private void ActualizarEstadisticas()
        {
            using var db = new TallerDbContext();
            TotalFacturas = db.Facturas.Count(f => f.Estado != "Anulada");
            TotalFacturado = db.Facturas.Where(f => f.Estado == "Pagada").Sum(f => f.Total);
        }

        private void NuevaFactura()
        {
            using var db = new TallerDbContext();

            // Buscar un trabajo finalizado sin factura
            var trabajoSinFactura = db.Trabajos
                .Include(t => t.Vehiculo)
                    .ThenInclude(v => v.Cliente)
                .Where(t => t.Estado == "Finalizado" && t.PrecioFinal != null)
                .FirstOrDefault(t => !db.Facturas.Any(f => f.TrabajoID == t.TrabajoID));

            if (trabajoSinFactura == null)
            {
                System.Windows.MessageBox.Show(
                    "No hay trabajos finalizados sin factura.",
                    "Sin Trabajos",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Generar número de factura
            var ultimaFactura = db.Facturas.OrderByDescending(f => f.FacturaID).FirstOrDefault();
            int numeroConsecutivo = ultimaFactura != null ? ultimaFactura.FacturaID + 1 : 1;
            string numeroFactura = $"FACT-{DateTime.Now.Year}-{numeroConsecutivo:D3}";

            // Calcular montos
            decimal subtotal = trabajoSinFactura.PrecioFinal ?? 0;
            decimal iva = subtotal * 0.13m; // IVA 13%
            decimal total = subtotal + iva;

            var nueva = new Factura
            {
                TrabajoID = trabajoSinFactura.TrabajoID,
                NumeroFactura = numeroFactura,
                FechaEmision = DateTime.Now,
                Subtotal = subtotal,
                Descuento = 0,
                IVA = iva,
                Total = total,
                Estado = "Pagada"
            };

            db.Facturas.Add(nueva);
            db.SaveChanges();

            var facturaConRelaciones = db.Facturas
                .Include(f => f.Trabajo)
                    .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Cliente)
                .FirstOrDefault(f => f.FacturaID == nueva.FacturaID);

            if (facturaConRelaciones != null)
            {
                Facturas.Insert(0, facturaConRelaciones);
            }

            ActualizarEstadisticas();

            System.Windows.MessageBox.Show(
                $"Factura {numeroFactura} creada exitosamente.\nTotal: Bs. {total:N2}",
                "Factura Creada",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        private void VerDetalle()
        {
            if (FacturaSeleccionada == null) return;

            var mensaje = $"FACTURA: {FacturaSeleccionada.NumeroFactura}\n" +
                         $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                         $"Fecha: {FacturaSeleccionada.FechaEmision:dd/MM/yyyy HH:mm}\n" +
                         $"Cliente: {FacturaSeleccionada.Trabajo?.Vehiculo?.Cliente?.Nombre} {FacturaSeleccionada.Trabajo?.Vehiculo?.Cliente?.Apellido}\n" +
                         $"Vehículo: {FacturaSeleccionada.Trabajo?.Vehiculo?.Marca} {FacturaSeleccionada.Trabajo?.Vehiculo?.Modelo}\n" +
                         $"Placa: {FacturaSeleccionada.Trabajo?.Vehiculo?.Placa}\n\n";

            if (!string.IsNullOrEmpty(FacturaSeleccionada.NIT))
            {
                mensaje += $"NIT: {FacturaSeleccionada.NIT}\n" +
                          $"Razón Social: {FacturaSeleccionada.RazonSocial}\n\n";
            }

            mensaje += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                      $"Subtotal:     Bs. {FacturaSeleccionada.Subtotal:N2}\n" +
                      $"Descuento:    Bs. {FacturaSeleccionada.Descuento:N2}\n" +
                      $"IVA (13%):    Bs. {FacturaSeleccionada.IVA:N2}\n" +
                      $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                      $"TOTAL:        Bs. {FacturaSeleccionada.Total:N2}\n\n" +
                      $"Estado: {FacturaSeleccionada.Estado}";

            System.Windows.MessageBox.Show(
                mensaje,
                "Detalle de Factura",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        private void ImprimirFactura()
        {
            if (FacturaSeleccionada == null) return;

            System.Windows.MessageBox.Show(
                $"Generando impresión de la factura:\n{FacturaSeleccionada.NumeroFactura}\n\n" +
                $"Esta funcionalidad se conectaría con un generador de PDF\n" +
                $"o se enviaría directamente a la impresora.",
                "Imprimir Factura",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        private void AnularFactura()
        {
            if (FacturaSeleccionada == null || FacturaSeleccionada.Estado == "Anulada")
                return;

            var resultado = System.Windows.MessageBox.Show(
                $"¿Está seguro de anular la factura {FacturaSeleccionada.NumeroFactura}?\n\n" +
                $"Esta acción no se puede deshacer.",
                "Anular Factura",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (resultado == System.Windows.MessageBoxResult.Yes)
            {
                using var db = new TallerDbContext();
                var factura = db.Facturas.Find(FacturaSeleccionada.FacturaID);

                if (factura != null)
                {
                    factura.Estado = "Anulada";
                    db.SaveChanges();

                    FacturaSeleccionada.Estado = "Anulada";
                    OnPropertyChanged(nameof(Facturas));
                    ActualizarEstadisticas();
                    CommandManager.InvalidateRequerySuggested();

                    System.Windows.MessageBox.Show(
                        "Factura anulada exitosamente.",
                        "Factura Anulada",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
        }

        private void EliminarFactura()
        {
            if (FacturaSeleccionada == null) return;

            var resultado = System.Windows.MessageBox.Show(
                $"¿Está seguro de eliminar la factura {FacturaSeleccionada.NumeroFactura}?\n\n" +
                $"ADVERTENCIA: Esta acción eliminará permanentemente el registro.",
                "Confirmar Eliminación",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (resultado == System.Windows.MessageBoxResult.Yes)
            {
                using var db = new TallerDbContext();
                var factura = db.Facturas.Find(FacturaSeleccionada.FacturaID);

                if (factura != null)
                {
                    db.Facturas.Remove(factura);
                    db.SaveChanges();
                    Facturas.Remove(FacturaSeleccionada);
                    ActualizarEstadisticas();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
