using Proyecto_taller.Data;
using Proyecto_taller.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.Views
{
    public partial class FinalizarTrabajoWindow : Window
    {
        private readonly int _trabajoId;
        private Trabajo _trabajo;

        // Subtotal calculado desde servicios + repuestos en BD
        private decimal _subtotalCalculado;

        // Precio base que el usuario puede editar (parte del subtotal o manual)
        private decimal PrecioBase
        {
            get
            {
                if (decimal.TryParse(txtPrecioBase.Text.Trim(), out decimal v) && v >= 0)
                    return v;
                return _subtotalCalculado;
            }
        }

        // Monto del descuento calculado
        private decimal MontoDescuento
        {
            get
            {
                if (chkDescuento?.IsChecked != true) return 0;
                if (!decimal.TryParse(txtDescuento?.Text?.Trim(), out decimal val) || val <= 0) return 0;

                if (rbPorcentaje?.IsChecked == true)
                    return Math.Round(PrecioBase * val / 100, 2);
                else
                    return Math.Min(val, PrecioBase); // monto fijo, no puede superar el precio base
            }
        }

        private decimal TotalFinal => Math.Max(0, PrecioBase - MontoDescuento);

        public FinalizarTrabajoWindow(int trabajoId)
        {
            InitializeComponent();
            _trabajoId = trabajoId;
            Loaded += (_, __) => CargarDatos();
        }

        // ─────────────────────────────────────────────────────────
        //  CARGA
        // ─────────────────────────────────────────────────────────

        private void CargarDatos()
        {
            try
            {
                using var db = new TallerDbContext();

                _trabajo = db.Trabajos
                    .Include(t => t.Vehiculo).ThenInclude(v => v.Cliente)
                    .Include(t => t.Servicios).ThenInclude(ts => ts.Servicio)
                    .Include(t => t.Repuestos).ThenInclude(tr => tr.Repuesto)
                    .FirstOrDefault(t => t.TrabajoID == _trabajoId);

                if (_trabajo == null) { Close(); return; }

                // Header
                txtTitulo.Text = $"✅  Finalizar Trabajo #{_trabajo.TrabajoID}";
                txtSubtitulo.Text = $"{_trabajo.Vehiculo?.Marca} {_trabajo.Vehiculo?.Modelo} · {_trabajo.Vehiculo?.Placa}";

                // Datos del cliente / trabajo
                var c = _trabajo.Vehiculo?.Cliente;
                txtCliente.Text = $"{c?.Nombre} {c?.Apellido}";
                txtVehiculo.Text = $"{_trabajo.Vehiculo?.Marca} {_trabajo.Vehiculo?.Modelo}";
                txtPlaca.Text = _trabajo.Vehiculo?.Placa ?? "—";
                txtTipoTrabajo.Text = _trabajo.TipoTrabajo;
                txtDescripcion.Text = string.IsNullOrWhiteSpace(_trabajo.Descripcion) ? "Sin descripción" : _trabajo.Descripcion;
                txtFechaIngreso.Text = $"Ingresó: {_trabajo.FechaIngreso:dd/MM/yyyy HH:mm}";

                // Servicios para el resumen
                var servicioItems = _trabajo.Servicios?
                    .Select(ts => new
                    {
                        NombreServicio = ts.Servicio?.Nombre ?? "—",
                        ts.Cantidad,
                        ts.Subtotal
                    }).ToList();
                dgServiciosResumen.ItemsSource = servicioItems;

                // Repuestos para el resumen
                var repuestoItems = _trabajo.Repuestos?
                    .Select(tr => new
                    {
                        NombreRepuesto = tr.Repuesto?.Nombre ?? "—",
                        tr.Cantidad,
                        tr.PrecioUnitario,
                        tr.Subtotal
                    }).ToList();
                dgRepuestosResumen.ItemsSource = repuestoItems;

                // Calcular subtotal base
                decimal totalSvc = _trabajo.Servicios?.Sum(s => s.Subtotal) ?? 0;
                decimal totalRep = _trabajo.Repuestos?.Sum(r => r.Subtotal) ?? 0;
                _subtotalCalculado = totalSvc + totalRep;

                // Si no hay servicios ni repuestos, usar precio estimado como fallback
                if (_subtotalCalculado == 0 && _trabajo.PrecioEstimado.HasValue)
                    _subtotalCalculado = _trabajo.PrecioEstimado.Value;

                lblSubtotalCalculado.Text = $"Bs. {_subtotalCalculado:N2}";

                // Precio base editable — pre-rellenar con el subtotal
                txtPrecioBase.Text = _subtotalCalculado.ToString("N2");

                RecalcularTotal(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  RECALCULAR EN TIEMPO REAL
        // ─────────────────────────────────────────────────────────

        private void RecalcularTotal(object sender, TextChangedEventArgs e)
            => ActualizarUI();

        private void RecalcularTotal(object sender, RoutedEventArgs e)
            => ActualizarUI();

        private void DescuentoToggle(object sender, RoutedEventArgs e)
        {
            if (panelDescuento == null) return;
            bool activo = chkDescuento.IsChecked == true;
            panelDescuento.IsEnabled = activo;
            panelDescuento.Opacity = activo ? 1.0 : 0.4;
            ActualizarUI();
        }

        private void ActualizarUI()
        {
            if (lblTotalFinal == null) return;

            decimal monto = MontoDescuento;

            if (monto > 0)
            {
                string tipoLabel = rbPorcentaje?.IsChecked == true
                    ? $"({txtDescuento?.Text?.Trim()}%)"
                    : "(monto fijo)";
                lblDescuentoCalc.Text = $"- Bs. {monto:N2}  {tipoLabel}";
                lblResumenDescuento.Text = $"- Bs. {monto:N2}";
            }
            else
            {
                lblDescuentoCalc.Text = "—";
                lblResumenDescuento.Text = "—";
            }

            lblResumenBase.Text = $"Bs. {PrecioBase:N2}";
            lblTotalFinal.Text = $"Bs. {TotalFinal:N2}";
        }

        // ─────────────────────────────────────────────────────────
        //  CONFIRMAR → única fuente de verdad para PrecioFinal
        // ─────────────────────────────────────────────────────────

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            // Validación mínima
            if (TotalFinal <= 0)
            {
                var cont = MessageBox.Show(
                    "El total final es Bs. 0.00.\n¿Está seguro de finalizar con este monto?",
                    "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (cont == MessageBoxResult.No) return;
            }

            // Confirmación final
            decimal descuento = MontoDescuento;
            string resumen =
                $"📋  RESUMEN DE FINALIZACIÓN\n\n" +
                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                $"👤  {txtCliente.Text}\n" +
                $"🚗  {txtVehiculo.Text}  ·  {txtPlaca.Text}\n" +
                $"🔧  {txtTipoTrabajo.Text}\n\n" +
                $"Servicios:     {_trabajo.Servicios?.Count ?? 0}\n" +
                $"Repuestos:     {_trabajo.Repuestos?.Count ?? 0}\n\n" +
                $"Precio base:   Bs. {PrecioBase:N2}\n" +
                (descuento > 0 ? $"Descuento:     - Bs. {descuento:N2}\n" : "") +
                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                $"TOTAL FINAL:   Bs. {TotalFinal:N2}\n\n" +
                $"¿Confirmar y cerrar el trabajo?";

            var r = MessageBox.Show(resumen, "Confirmar Finalización",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r != MessageBoxResult.Yes) return;

            try
            {
                using var db = new TallerDbContext();

                var trabajo = db.Trabajos
                    .Include(t => t.Servicios)
                    .Include(t => t.Repuestos)
                    .FirstOrDefault(t => t.TrabajoID == _trabajoId);

                if (trabajo == null) return;

                // ╔══════════════════════════════════════════════════╗
                // ║  ÚNICA FUENTE DE VERDAD — PrecioFinal se asigna  ║
                // ║  SOLO aquí, en la finalización                   ║
                // ╚══════════════════════════════════════════════════╝
                trabajo.Estado = "Finalizado";
                trabajo.FechaEntrega = DateTime.Now;
                trabajo.PrecioFinal = TotalFinal;

                db.SaveChanges();

                // Mostrar resumen final en MessageBox informativo
                string resultado =
                    $"✅  TRABAJO FINALIZADO EXITOSAMENTE\n\n" +
                    $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                    $"📋  Trabajo #:      {trabajo.TrabajoID}\n" +
                    $"👤  Cliente:        {txtCliente.Text}\n" +
                    $"🚗  Vehículo:       {txtVehiculo.Text}\n" +
                    $"🔑  Placa:          {txtPlaca.Text}\n" +
                    $"🔧  Tipo:           {txtTipoTrabajo.Text}\n\n" +
                    $"📅  Ingreso:        {_trabajo.FechaIngreso:dd/MM/yyyy}\n" +
                    $"📅  Finalización:   {DateTime.Now:dd/MM/yyyy HH:mm}\n\n" +
                    $"⚙️   Servicios:      {_trabajo.Servicios?.Count ?? 0} item(s)\n" +
                    $"📦  Repuestos:      {_trabajo.Repuestos?.Count ?? 0} item(s)\n\n";

                if (_subtotalCalculado != PrecioBase)
                    resultado += $"Subtotal calc.:  Bs. {_subtotalCalculado:N2}\n" +
                                 $"Precio ajustado: Bs. {PrecioBase:N2}\n";
                else
                    resultado += $"Subtotal:        Bs. {PrecioBase:N2}\n";

                if (descuento > 0)
                    resultado += $"Descuento:       - Bs. {descuento:N2}\n";

                resultado +=
                    $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                    $"💰  TOTAL COBRADO:  Bs. {TotalFinal:N2}\n\n" +
                    $"💡  Puede generar la factura desde el módulo Facturación.";

                MessageBox.Show(resultado, "Trabajo Finalizado",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al finalizar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
