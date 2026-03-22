using Proyecto_taller.Data;
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
    public partial class DetallesTrabajoWindow : Window
    {
        private readonly int _trabajoId;

        public DetallesTrabajoWindow(int trabajoId)
        {
            InitializeComponent();
            _trabajoId = trabajoId;
            Loaded += (_, __) => CargarDatos();
        }

        private void CargarDatos()
        {
            try
            {
                using var db = new TallerDbContext();

                var trabajo = db.Trabajos
                    .Include(t => t.Vehiculo).ThenInclude(v => v.Cliente)
                    .Include(t => t.Servicios).ThenInclude(ts => ts.Servicio)
                    .Include(t => t.Repuestos).ThenInclude(tr => tr.Repuesto)
                    .FirstOrDefault(t => t.TrabajoID == _trabajoId);

                if (trabajo == null) { Close(); return; }

                // ── Header ────────────────────────────────────────
                txtTituloHeader.Text = $"📋  Trabajo #{trabajo.TrabajoID}";
                txtSubheader.Text = $"{trabajo.Vehiculo?.Marca} {trabajo.Vehiculo?.Modelo}  ·  {trabajo.Vehiculo?.Cliente?.Nombre} {trabajo.Vehiculo?.Cliente?.Apellido}";

                // Badge de estado
                (txtEstadoBadge.Text, badgeEstado.Background, txtEstadoBadge.Foreground) = trabajo.Estado switch
                {
                    "Pendiente" => ("⏳  Pendiente", new SolidColorBrush(Color.FromRgb(61, 46, 24)), new SolidColorBrush(Color.FromRgb(245, 158, 11))),
                    "En Progreso" => ("🔄  En Progreso", new SolidColorBrush(Color.FromRgb(26, 37, 53)), new SolidColorBrush(Color.FromRgb(59, 130, 246))),
                    "Finalizado" => ("✅  Finalizado", new SolidColorBrush(Color.FromRgb(24, 40, 32)), new SolidColorBrush(Color.FromRgb(16, 185, 129))),
                    _ => (trabajo.Estado, new SolidColorBrush(Color.FromRgb(49, 46, 107)), new SolidColorBrush(Color.FromRgb(129, 140, 248)))
                };

                // ── Cliente ───────────────────────────────────────
                var c = trabajo.Vehiculo?.Cliente;
                txtClienteNombre.Text = $"{c?.Nombre} {c?.Apellido}";
                txtClienteTelefono.Text = c?.Telefono ?? "—";
                txtClienteEmail.Text = string.IsNullOrWhiteSpace(c?.Correo) ? "—" : c.Correo;

                // ── Vehículo ──────────────────────────────────────
                var v = trabajo.Vehiculo;
                txtVehiculoNombre.Text = $"{v?.Marca} {v?.Modelo}";
                txtVehiculoPlaca.Text = v?.Placa ?? "—";
                txtVehiculoAnio.Text = v?.Anio?.ToString() ?? "—";

                // ── Trabajo ───────────────────────────────────────
                txtTipoTrabajo.Text = trabajo.TipoTrabajo;
                txtDescripcion.Text = string.IsNullOrWhiteSpace(trabajo.Descripcion) ? "Sin descripción" : trabajo.Descripcion;
                txtFechaIngreso.Text = trabajo.FechaIngreso.ToString("dd/MM/yyyy HH:mm");
                txtFechaEntrega.Text = trabajo.FechaEntrega.HasValue ? trabajo.FechaEntrega.Value.ToString("dd/MM/yyyy HH:mm") : "—";
                txtPrecioEstimado.Text = trabajo.PrecioEstimado.HasValue ? $"Bs. {trabajo.PrecioEstimado:N2}" : "—";
                txtPrecioFinal.Text = trabajo.PrecioFinal.HasValue ? $"Bs. {trabajo.PrecioFinal:N2}" : "Pendiente";

                // ── Servicios ─────────────────────────────────────
                var servicioItems = trabajo.Servicios?
                    .Select(ts => new
                    {
                        NombreServicio = ts.Servicio?.Nombre ?? "—",
                        ts.Cantidad,
                        ts.Subtotal
                    }).ToList();

                dgServicios.ItemsSource = servicioItems;
                decimal totalSvc = servicioItems?.Sum(s => s.Subtotal) ?? 0;
                txtTotalServicios.Text = $"— Bs. {totalSvc:N2}";

                // ── Repuestos ─────────────────────────────────────
                var repuestoItems = trabajo.Repuestos?
                    .Select(tr => new
                    {
                        NombreRepuesto = tr.Repuesto?.Nombre ?? "—",
                        tr.Cantidad,
                        tr.PrecioUnitario,
                        tr.Subtotal
                    }).ToList();

                dgRepuestos.ItemsSource = repuestoItems;
                decimal totalRep = repuestoItems?.Sum(r => r.Subtotal) ?? 0;
                txtTotalRepuestos.Text = $"— Bs. {totalRep:N2}";

                // ── Resumen financiero (solo si finalizado) ───────
                if (trabajo.Estado == "Finalizado" && trabajo.PrecioFinal.HasValue)
                {
                    panelResumenFinanciero.Visibility = Visibility.Visible;

                    // Para mostrar el ajuste manual necesitamos los datos que
                    // guarda FinalizarTrabajoWindow. Los inferimos:
                    // subtotalBruto = servicios + repuestos
                    decimal subtotalBruto = totalSvc + totalRep;
                    // Si el precio estimado fue el fallback, ajuste = 0
                    decimal ajuste = 0;
                    // Buscamos la factura por si hay descuento registrado ahí
                    var factura = db.Facturas.FirstOrDefault(f => f.TrabajoID == trabajo.TrabajoID);
                    decimal descuento = factura?.Descuento ?? 0;

                    txtResumenServicios.Text = $"Bs. {totalSvc:N2}";
                    txtResumenRepuestos.Text = $"Bs. {totalRep:N2}";
                    txtResumenAjuste.Text = ajuste != 0 ? $"Bs. {ajuste:N2}" : "—";
                    txtResumenDescuento.Text = descuento > 0 ? $"- Bs. {descuento:N2}" : "—";
                    txtResumenTotal.Text = $"Bs. {trabajo.PrecioFinal:N2}";
                }
                else
                {
                    panelResumenFinanciero.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar detalles:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e) => Close();
    }
}

