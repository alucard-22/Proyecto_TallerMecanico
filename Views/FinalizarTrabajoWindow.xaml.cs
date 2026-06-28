using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.Views
{
    public partial class FinalizarTrabajoWindow : Window
    {
        private readonly int _trabajoId;
        private Trabajo _trabajo;
        private decimal _subtotalCalculado;
        private decimal _descuentoMaximoPorcentaje;

        // ── NUEVO: anticipo que el cliente dejó al ingresar el vehículo ──────
        private decimal _anticipoRecibido;

        private decimal PrecioBase
        {
            get
            {
                if (decimal.TryParse(txtPrecioBase.Text.Trim(), out decimal v) && v >= 0)
                    return v;
                return _subtotalCalculado;
            }
        }

        private decimal MontoDescuento
        {
            get
            {
                if (chkDescuento?.IsChecked != true) return 0;
                if (!decimal.TryParse(txtDescuento?.Text?.Trim(), out decimal val) || val <= 0) return 0;

                if (rbPorcentaje?.IsChecked == true)
                    return Math.Round(PrecioBase * val / 100, 2);
                else
                    return Math.Min(val, PrecioBase);
            }
        }

        private decimal PorcentajeDescuentoEfectivo
        {
            get
            {
                if (PrecioBase <= 0) return 0;
                return Math.Round(MontoDescuento / PrecioBase * 100, 2);
            }
        }

        private bool SuperaDescuentoMaximo =>
            MontoDescuento > 0 && PorcentajeDescuentoEfectivo > _descuentoMaximoPorcentaje;

        // Total después de descuento, antes de restar el anticipo
        private decimal TotalConDescuento => Math.Max(0, PrecioBase - MontoDescuento);

        // ── NUEVO: saldo real a cobrar, después de restar el anticipo ────────
        // Si el anticipo es mayor al total (caso raro pero posible si se
        // aplicó un descuento grande), el saldo nunca baja de 0 — no se le
        // "debe" dinero al cliente desde esta pantalla.
        private decimal TotalFinalValor => Math.Max(0, TotalConDescuento - _anticipoRecibido);

        public FinalizarTrabajoWindow(int trabajoId)
        {
            InitializeComponent();
            _trabajoId = trabajoId;
            Loaded += (_, __) => CargarDatos();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CargarDatos()
        {
            try
            {
                var config = ConfiguracionHelper.CargarConfiguracion();
                _descuentoMaximoPorcentaje = config.DescuentoMaximo;

                using var db = new TallerDbContext();

                _trabajo = db.Trabajos
                    .Include(t => t.Vehiculo).ThenInclude(v => v.Cliente)
                    .Include(t => t.Servicios).ThenInclude(ts => ts.Servicio)
                    .Include(t => t.Repuestos).ThenInclude(tr => tr.Repuesto)
                    .Include(t => t.UsuarioAsignado)
                    .FirstOrDefault(t => t.TrabajoID == _trabajoId);

                if (_trabajo == null) { Close(); return; }

                txtTitulo.Text = $"✅  Finalizar Trabajo #{_trabajo.TrabajoID}";
                txtSubtitulo.Text = $"{_trabajo.Vehiculo?.Marca} {_trabajo.Vehiculo?.Modelo} · {_trabajo.Vehiculo?.Placa}";

                var c = _trabajo.Vehiculo?.Cliente;
                txtCliente.Text = $"{c?.Nombre} {c?.Apellido}";
                txtVehiculo.Text = $"{_trabajo.Vehiculo?.Marca} {_trabajo.Vehiculo?.Modelo}";
                txtPlaca.Text = _trabajo.Vehiculo?.Placa ?? "—";
                txtTipoTrabajo.Text = _trabajo.TipoTrabajo;
                txtDescripcion.Text = string.IsNullOrWhiteSpace(_trabajo.Descripcion) ? "Sin descripción" : _trabajo.Descripcion;
                txtFechaIngreso.Text = $"Ingresó: {_trabajo.FechaIngreso:dd/MM/yyyy HH:mm}";
                txtEmpleadoAsignado.Text = _trabajo.UsuarioAsignado != null
                    ? $"Asignado a: {_trabajo.UsuarioAsignado.NombreCompleto}"
                    : "Sin empleado asignado";

                var servicioItems = _trabajo.Servicios?
                    .Select(ts => new
                    {
                        NombreServicio = ts.Servicio?.Nombre ?? "—",
                        ts.Cantidad,
                        ts.Subtotal
                    }).ToList();
                dgServiciosResumen.ItemsSource = servicioItems;

                var repuestoItems = _trabajo.Repuestos?
                    .Select(tr => new
                    {
                        NombreRepuesto = tr.Repuesto?.Nombre ?? "—",
                        tr.Cantidad,
                        tr.PrecioUnitario,
                        tr.Subtotal
                    }).ToList();
                dgRepuestosResumen.ItemsSource = repuestoItems;

                decimal totalSvc = _trabajo.Servicios?.Sum(s => s.Subtotal) ?? 0;
                decimal totalRep = _trabajo.Repuestos?.Sum(r => r.Subtotal) ?? 0;
                decimal subtotalServiciosRepuestos = totalSvc + totalRep;

                // Si en GestionarTrabajoWindow se guardó un precio manual
                // distinto al subtotal de servicios+repuestos, ese ajuste
                // prevalece aquí como punto de partida.
                _subtotalCalculado = _trabajo.PrecioEstimado.HasValue
                    ? _trabajo.PrecioEstimado.Value
                    : subtotalServiciosRepuestos;

                lblSubtotalCalculado.Text = $"Bs. {_subtotalCalculado:N2}";
                txtPrecioBase.Text = _subtotalCalculado.ToString("N2");

                // ── NUEVO: cargar y mostrar el anticipo recibido ────────────────
                _anticipoRecibido = _trabajo.Anticipo;
                lblAnticipoRecibido.Text = $"Bs. {_anticipoRecibido:N2}";

                ActualizarUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RecalcularTotal(object sender, TextChangedEventArgs e) => ActualizarUI();
        private void RecalcularTotal(object sender, RoutedEventArgs e) => ActualizarUI();

        private void DescuentoToggle(object sender, RoutedEventArgs e)
        {
            if (panelDescuento == null) return;
            bool activo = chkDescuento.IsChecked == true;
            panelDescuento.IsEnabled = activo;
            panelDescuento.Opacity = activo ? 1.0 : 0.4;
            ActualizarUI();
        }

        private void ActualizarTextoLimiteDescuento()
        {
            if (lblDescuentoCalc == null) return;

            if (SuperaDescuentoMaximo)
            {
                lblDescuentoCalc.Text =
                    $"⚠️  {PorcentajeDescuentoEfectivo:N1}% supera el máximo permitido " +
                    $"({_descuentoMaximoPorcentaje:N0}%)";
                lblDescuentoCalc.Foreground = (System.Windows.Media.Brush)FindResource("ColorDanger");
            }
        }

        private void ActualizarUI()
        {
            if (lblTotalFinal == null) return;

            decimal monto = MontoDescuento;

            if (monto > 0)
            {
                if (SuperaDescuentoMaximo)
                {
                    ActualizarTextoLimiteDescuento();
                }
                else
                {
                    string tipoLabel = rbPorcentaje?.IsChecked == true
                        ? $"({txtDescuento?.Text?.Trim()}%)"
                        : "(monto fijo)";
                    lblDescuentoCalc.Text = $"- Bs. {monto:N2}  {tipoLabel}";
                    lblDescuentoCalc.Foreground = (System.Windows.Media.Brush)FindResource("ColorDanger");
                }
                lblResumenDescuento.Text = $"- Bs. {monto:N2}";
            }
            else
            {
                lblDescuentoCalc.Text = "—";
                lblDescuentoCalc.Foreground = (System.Windows.Media.Brush)FindResource("ColorDanger");
                lblResumenDescuento.Text = "—";
            }

            lblResumenBase.Text = $"Bs. {PrecioBase:N2}";

            // ── NUEVO: mostrar el anticipo en el resumen y el saldo restante ──
            lblResumenAnticipo.Text = _anticipoRecibido > 0
                ? $"- Bs. {_anticipoRecibido:N2}"
                : "—";

            lblTotalFinal.Text = $"Bs. {TotalFinalValor:N2}";

            if (_anticipoRecibido > 0)
            {
                if (TotalConDescuento <= _anticipoRecibido)
                    lblNotaAnticipo.Text = "El anticipo cubre el total — saldo Bs. 0.00";
                else
                    lblNotaAnticipo.Text = $"Total con descuento Bs. {TotalConDescuento:N2} menos anticipo Bs. {_anticipoRecibido:N2}";
            }
            else
            {
                lblNotaAnticipo.Text = "";
            }
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            if (SuperaDescuentoMaximo)
            {
                MessageBox.Show(
                    $"El descuento aplicado ({PorcentajeDescuentoEfectivo:N1}%) supera el " +
                    $"máximo permitido configurado en el sistema ({_descuentoMaximoPorcentaje:N0}%).\n\n" +
                    $"Ajusta el descuento o solicita a un administrador que modifique el " +
                    $"límite desde Configuración.",
                    "Descuento no permitido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDescuento.Focus();
                return;
            }

            if (TotalFinalValor <= 0 && _anticipoRecibido == 0)
            {
                var cont = MessageBox.Show(
                    "El saldo a cobrar es Bs. 0.00.\n¿Está seguro de finalizar con este monto?",
                    "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (cont == MessageBoxResult.No) return;
            }

            decimal descuento = MontoDescuento;
            string resumen =
                $"📋  RESUMEN DE FINALIZACIÓN\n\n" +
                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                $"👤  {txtCliente.Text}\n" +
                $"🚗  {txtVehiculo.Text}  ·  {txtPlaca.Text}\n" +
                $"🔧  {txtTipoTrabajo.Text}\n\n" +
                $"Precio base:    Bs. {PrecioBase:N2}\n" +
                (descuento > 0 ? $"Descuento:      - Bs. {descuento:N2}  ({PorcentajeDescuentoEfectivo:N1}%)\n" : "") +
                (_anticipoRecibido > 0 ? $"Anticipo:       - Bs. {_anticipoRecibido:N2}\n" : "") +
                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                $"SALDO A COBRAR: Bs. {TotalFinalValor:N2}\n\n" +
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

                trabajo.Estado = "Finalizado";
                trabajo.FechaEntrega = DateTime.Now;

                // ── NOTA IMPORTANTE: PrecioFinal guarda el TOTAL del servicio
                // (precio base - descuento), NO el saldo después del anticipo.
                // Esto es deliberado: PrecioFinal representa el valor real del
                // trabajo realizado, que es lo que debe aparecer en reportes,
                // estadísticas de ingresos y el recibo/factura. El anticipo ya
                // fue contabilizado como un pago separado (ver Pagos), así que
                // restarlo de PrecioFinal duplicaría el descuento contable.
                trabajo.PrecioFinal = TotalConDescuento;

                db.SaveChanges();

                // ── NUEVO: registrar el anticipo como un pago, si existe ───────
                if (_anticipoRecibido > 0)
                {
                    db.Pagos.Add(new Pago
                    {
                        TrabajoID = trabajo.TrabajoID,
                        Monto = _anticipoRecibido,
                        MetodoPago = "Anticipo",
                        FechaPago = _trabajo.FechaIngreso
                    });
                    db.SaveChanges();
                }

                // ── NUEVO: registrar en auditoría ──────────────────────────────
                AuditoriaHelper.Registrar(
                    "Finalizar", "Trabajo", trabajo.TrabajoID,
                    $"Trabajo #{trabajo.TrabajoID} finalizado. Total: Bs. {TotalConDescuento:N2}" +
                    (_anticipoRecibido > 0 ? $", anticipo aplicado Bs. {_anticipoRecibido:N2}, saldo Bs. {TotalFinalValor:N2}" : ""));

                MessageBox.Show(
                    $"✅  TRABAJO FINALIZADO EXITOSAMENTE\n\n" +
                    (_anticipoRecibido > 0
                        ? $"💰  TOTAL DEL TRABAJO:  Bs. {TotalConDescuento:N2}\n" +
                          $"💵  ANTICIPO APLICADO:  Bs. {_anticipoRecibido:N2}\n" +
                          $"💲  SALDO COBRADO:      Bs. {TotalFinalValor:N2}\n\n"
                        : $"💰  TOTAL COBRADO:  Bs. {TotalFinalValor:N2}\n\n") +
                    $"💡  Puede generar la factura desde el módulo Facturación.",
                    "Trabajo Finalizado",
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
