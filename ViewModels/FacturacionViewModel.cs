using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.EntityFrameworkCore;
using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using Proyecto_taller.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Proyecto_taller.ViewModels
{
    public class FacturacionViewModel : INotifyPropertyChanged
    {
        // ─────────────────────────────────────────────────────────
        //  ESTADO
        // ─────────────────────────────────────────────────────────

        private Factura _facturaSeleccionada;
        private bool _filtroTodas = true;
        private bool _filtroPagadas;
        private bool _filtroPendientes;
        private bool _filtroEsteMes;
        private decimal _totalFacturado;
        private int _totalFacturas;

        public ObservableCollection<Factura> Facturas { get; set; } = new();

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
            set { _totalFacturado = value; OnPropertyChanged(); }
        }

        public int TotalFacturas
        {
            get => _totalFacturas;
            set { _totalFacturas = value; OnPropertyChanged(); }
        }

        // ── Filtros ───────────────────────────────────────────────
        public bool FiltroTodas
        {
            get => _filtroTodas;
            set { _filtroTodas = value; OnPropertyChanged(); if (value) Recargar(); }
        }
        public bool FiltroPagadas
        {
            get => _filtroPagadas;
            set { _filtroPagadas = value; OnPropertyChanged(); if (value) Recargar("Pagada"); }
        }
        public bool FiltroPendientes
        {
            get => _filtroPendientes;
            set { _filtroPendientes = value; OnPropertyChanged(); if (value) Recargar("Pendiente"); }
        }
        public bool FiltroEsteMes
        {
            get => _filtroEsteMes;
            set { _filtroEsteMes = value; OnPropertyChanged(); if (value) Recargar("EsteMes"); }
        }

        // ─────────────────────────────────────────────────────────
        //  COMANDOS
        // ─────────────────────────────────────────────────────────

        public ICommand CargarFacturasCommand { get; }
        public ICommand NuevaFacturaCommand { get; }
        public ICommand VerDetalleCommand { get; }
        public ICommand ImprimirFacturaCommand { get; }
        public ICommand AnularFacturaCommand { get; }
        public ICommand EliminarFacturaCommand { get; }

        public FacturacionViewModel()
        {
            CargarFacturasCommand = new RelayCommand(() => Recargar());
            NuevaFacturaCommand = new RelayCommand(NuevaFactura);
            VerDetalleCommand = new RelayCommand(VerDetalle, () => FacturaSeleccionada != null);
            ImprimirFacturaCommand = new RelayCommand(ImprimirFactura, () => FacturaSeleccionada != null);
            AnularFacturaCommand = new RelayCommand(AnularFactura, () => FacturaSeleccionada != null
                                                                           && FacturaSeleccionada.Estado != "Anulada");
            EliminarFacturaCommand = new RelayCommand(EliminarFactura, () => FacturaSeleccionada != null);

            Recargar();
        }

        // ─────────────────────────────────────────────────────────
        //  CARGA
        // ─────────────────────────────────────────────────────────

        private void Recargar(string filtro = null)
        {
            Facturas.Clear();

            using var db = new TallerDbContext();

            var query = db.Facturas
                .Include(f => f.Trabajo)
                    .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Cliente)
                .AsQueryable();

            switch (filtro)
            {
                case "Pagada":
                    query = query.Where(f => f.Estado == "Pagada");
                    break;
                case "Pendiente":
                    query = query.Where(f => f.Estado == "Pendiente");
                    break;
                case "EsteMes":
                    var inicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    query = query.Where(f => f.FechaEmision >= inicio);
                    break;
            }

            foreach (var f in query.OrderByDescending(f => f.FechaEmision).ToList())
                Facturas.Add(f);

            ActualizarEstadisticas();
        }

        private void ActualizarEstadisticas()
        {
            using var db = new TallerDbContext();
            TotalFacturas = db.Facturas.Count(f => f.Estado != "Anulada");
            TotalFacturado = db.Facturas
                .Where(f => f.Estado == "Pagada")
                .Sum(f => (decimal?)f.Total) ?? 0;
        }

        // ─────────────────────────────────────────────────────────
        //  NUEVA FACTURA — con diálogo de selección
        // ─────────────────────────────────────────────────────────

        private void NuevaFactura()
        {
            // Abrir ventana de selección de trabajo
            var dlg = new SeleccionarTrabajoWindow();
            if (dlg.ShowDialog() != true || dlg.TrabajoSeleccionado == null)
                return;

            var trabajo = dlg.TrabajoSeleccionado;

            try
            {
                using var db = new TallerDbContext();

                // Verificar que el trabajo aún no tiene factura
                // (podría haberse creado mientras el diálogo estaba abierto)
                if (db.Facturas.Any(f => f.TrabajoID == trabajo.TrabajoID))
                {
                    MessageBox.Show("Este trabajo ya tiene una factura asociada.\nActualiza la lista e intenta de nuevo.",
                        "Factura existente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Recargar();
                    return;
                }

                // ── Generar número de factura de forma segura ────────────────
                // Usamos el año + un correlativo basado en el COUNT real de facturas del año,
                // evitando la race condition del MAX(ID)+1.
                int anio = DateTime.Now.Year;
                int count = db.Facturas.Count(f => f.FechaEmision.Year == anio) + 1;
                string numeroFactura = $"FACT-{anio}-{count:D4}";

                // Aseguramos unicidad en caso de colisión
                while (db.Facturas.Any(f => f.NumeroFactura == numeroFactura))
                {
                    count++;
                    numeroFactura = $"FACT-{anio}-{count:D4}";
                }

                decimal subtotal = trabajo.PrecioFinal ?? 0;

                var nueva = new Factura
                {
                    TrabajoID = trabajo.TrabajoID,
                    NumeroFactura = numeroFactura,
                    FechaEmision = DateTime.Now,
                    Subtotal = subtotal,
                    Descuento = 0,
                    Total = subtotal,
                    Estado = "Pagada"
                };

                db.Facturas.Add(nueva);
                db.SaveChanges();

                // Recargar con relaciones para mostrar en grid
                var facturaCompleta = db.Facturas
                    .Include(f => f.Trabajo)
                        .ThenInclude(t => t.Vehiculo)
                            .ThenInclude(v => v.Cliente)
                    .First(f => f.FacturaID == nueva.FacturaID);

                Facturas.Insert(0, facturaCompleta);
                FacturaSeleccionada = facturaCompleta;
                ActualizarEstadisticas();

                MessageBox.Show(
                    $"✅  FACTURA CREADA EXITOSAMENTE\n\n" +
                    $"📄  {numeroFactura}\n" +
                    $"👤  {trabajo.Vehiculo?.Cliente?.Nombre} {trabajo.Vehiculo?.Cliente?.Apellido}\n" +
                    $"🚗  {trabajo.Vehiculo?.Marca} {trabajo.Vehiculo?.Modelo}\n\n" +
                    $"Subtotal:  Bs. {subtotal:N2}\n" +
                    $"Descuento: Bs. 0.00\n" +
                    $"━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                    $"TOTAL:     Bs. {subtotal:N2}",
                    "Factura Creada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear la factura:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  VER DETALLE — abre ventana real, no MessageBox
        // ─────────────────────────────────────────────────────────

        private void VerDetalle()
        {
            if (FacturaSeleccionada == null) return;
            var win = new DetallesFacturaWindow(FacturaSeleccionada.FacturaID);
            win.ShowDialog();
        }

        // ─────────────────────────────────────────────────────────
        //  IMPRIMIR → genera PDF
        // ─────────────────────────────────────────────────────────

        private void ImprimirFactura()
        {
            if (FacturaSeleccionada == null) return;

            // Recargar con relaciones completas antes de generar el PDF
            using var db = new TallerDbContext();
            var facturaCompleta = db.Facturas
                .Include(f => f.Trabajo)
                    .ThenInclude(t => t.Vehiculo)
                        .ThenInclude(v => v.Cliente)
                .FirstOrDefault(f => f.FacturaID == FacturaSeleccionada.FacturaID);

            if (facturaCompleta == null) return;
            FacturacionPdfHelper.GenerarPdf(facturaCompleta);
        }

        // ─────────────────────────────────────────────────────────
        //  ANULAR — actualiza grid correctamente
        // ─────────────────────────────────────────────────────────

        private void AnularFactura()
        {
            if (FacturaSeleccionada == null || FacturaSeleccionada.Estado == "Anulada") return;

            var r = MessageBox.Show(
                $"¿Anular la factura {FacturaSeleccionada.NumeroFactura}?\n\nEsta acción no se puede deshacer.",
                "Anular Factura", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (r != MessageBoxResult.Yes) return;

            try
            {
                using var db = new TallerDbContext();
                var f = db.Facturas.Find(FacturaSeleccionada.FacturaID);
                if (f == null) return;

                f.Estado = "Anulada";
                db.SaveChanges();

                // Actualizar la ObservableCollection correctamente:
                // Reemplazar el objeto en la colección para que el DataGrid lo refleje
                int idx = Facturas.IndexOf(FacturaSeleccionada);
                if (idx >= 0)
                {
                    // Recargar la factura con relaciones
                    var facturaActualizada = db.Facturas
                        .Include(fa => fa.Trabajo)
                            .ThenInclude(t => t.Vehiculo)
                                .ThenInclude(v => v.Cliente)
                        .First(fa => fa.FacturaID == f.FacturaID);

                    Facturas[idx] = facturaActualizada;
                    FacturaSeleccionada = facturaActualizada;
                }

                ActualizarEstadisticas();
                CommandManager.InvalidateRequerySuggested();

                MessageBox.Show("Factura anulada correctamente.",
                    "Anulada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al anular:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  ELIMINAR
        // ─────────────────────────────────────────────────────────

        private void EliminarFactura()
        {
            if (FacturaSeleccionada == null) return;

            var r = MessageBox.Show(
                $"¿Eliminar la factura {FacturaSeleccionada.NumeroFactura}?\n\n" +
                $"⚠️ Esta acción es permanente y no se puede deshacer.",
                "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (r != MessageBoxResult.Yes) return;

            try
            {
                using var db = new TallerDbContext();
                var f = db.Facturas.Find(FacturaSeleccionada.FacturaID);
                if (f != null) { db.Facturas.Remove(f); db.SaveChanges(); }

                Facturas.Remove(FacturaSeleccionada);
                FacturaSeleccionada = null;
                ActualizarEstadisticas();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  INotifyPropertyChanged
        // ─────────────────────────────────────────────────────────

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}