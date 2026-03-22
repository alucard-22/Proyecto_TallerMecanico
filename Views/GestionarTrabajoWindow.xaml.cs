using Proyecto_taller.Data;
using Proyecto_taller.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.Views
{
    public partial class GestionarTrabajoWindow : Window
    {
        private readonly int _trabajoId;
        private Trabajo _trabajo;

        // Listas en memoria que el usuario edita
        private List<Servicio> _todosLosServicios = new();
        private List<Repuesto> _todosLosRepuestos = new();
        private List<ServicioItem> _serviciosTrabajo = new();
        private List<RepuestoItem> _repuestosTrabajo = new();

        // ── Subtotales calculados ─────────────────────────────
        private decimal TotalServicios => _serviciosTrabajo.Sum(s => s.Subtotal);
        private decimal TotalRepuestos => _repuestosTrabajo.Sum(r => r.Subtotal);
        private decimal Subtotal => TotalServicios + TotalRepuestos;

        /// <summary>
        /// Precio que se guardará: el manual si lo ingresó el usuario, o el subtotal calculado.
        /// </summary>
        private decimal PrecioAGuardar
        {
            get
            {
                if (decimal.TryParse(txtPrecioManual.Text.Trim(), out decimal manual) && manual > 0)
                    return manual;
                return Subtotal;
            }
        }

        public GestionarTrabajoWindow(int trabajoId)
        {
            InitializeComponent();
            _trabajoId = trabajoId;
            Loaded += (_, __) => CargarDatos();
        }

        // ─────────────────────────────────────────────────────────
        //  CARGA INICIAL
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
                txtTitulo.Text = $"🔧  Gestionar Trabajo #{_trabajo.TrabajoID}";
                txtSubtitulo.Text = $"{_trabajo.Vehiculo?.Marca} {_trabajo.Vehiculo?.Modelo} · {_trabajo.Vehiculo?.Placa}  —  {_trabajo.Vehiculo?.Cliente?.Nombre} {_trabajo.Vehiculo?.Cliente?.Apellido}";

                // Catalogo disponible
                _todosLosServicios = db.Servicios.OrderBy(s => s.Nombre).ToList();
                // Repuestos disponibles = todos, incluyendo los ya asignados
                // (para que al reeditar se muestre el stock real devuelto)
                _todosLosRepuestos = db.Repuestos.OrderBy(r => r.Nombre).ToList();

                dgServiciosDisponibles.ItemsSource = _todosLosServicios;
                dgRepuestosDisponibles.ItemsSource = _todosLosRepuestos;

                // Servicios ya asignados
                if (_trabajo.Servicios != null)
                    foreach (var ts in _trabajo.Servicios)
                        _serviciosTrabajo.Add(new ServicioItem
                        {
                            ServicioID = ts.ServicioID,
                            NombreServicio = ts.Servicio?.Nombre ?? "—",
                            Cantidad = ts.Cantidad,
                            CostoUnitario = ts.Servicio?.CostoBase ?? 0,
                            Subtotal = ts.Subtotal
                        });

                // Repuestos ya asignados
                if (_trabajo.Repuestos != null)
                    foreach (var tr in _trabajo.Repuestos)
                        _repuestosTrabajo.Add(new RepuestoItem
                        {
                            RepuestoID = tr.RepuestoID,
                            NombreRepuesto = tr.Repuesto?.Nombre ?? "—",
                            Cantidad = tr.Cantidad,
                            PrecioUnitario = tr.PrecioUnitario,
                            Subtotal = tr.Subtotal
                        });

                // Estado
                SetComboEstado(_trabajo.Estado);
                dpFechaEntrega.SelectedDate = _trabajo.FechaEntrega ?? DateTime.Now.AddDays(3);
                txtNotas.Text = _trabajo.Descripcion ?? "";

                // Precio manual solo si había uno guardado (no por defecto)
                // Lo dejamos vacío para no confundir con el subtotal calculado
                txtPrecioManual.Text = "";

                ActualizarTotales();
                RefrescarGrids();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  AGREGAR / ELIMINAR SERVICIOS
        // ─────────────────────────────────────────────────────────

        private void AgregarServicio_Click(object sender, RoutedEventArgs e)
        {
            var svc = dgServiciosDisponibles.SelectedItem as Servicio;
            if (svc == null)
            {
                MessageBox.Show("Seleccione un servicio.", "Advertencia",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            if (!int.TryParse(txtCantidadServicio.Text, out int cant) || cant <= 0)
            {
                MessageBox.Show("Cantidad inválida.", "Advertencia",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            var existente = _serviciosTrabajo.FirstOrDefault(s => s.ServicioID == svc.ServicioID);
            if (existente != null)
            {
                existente.Cantidad += cant;
                existente.Subtotal = existente.Cantidad * existente.CostoUnitario;
            }
            else
            {
                _serviciosTrabajo.Add(new ServicioItem
                {
                    ServicioID = svc.ServicioID,
                    NombreServicio = svc.Nombre,
                    Cantidad = cant,
                    CostoUnitario = svc.CostoBase,
                    Subtotal = cant * svc.CostoBase
                });
            }

            txtCantidadServicio.Text = "1";
            RefrescarGrids();
            ActualizarTotales();
        }

        private void EliminarServicio_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.DataContext as ServicioItem;
            if (item == null) return;
            _serviciosTrabajo.Remove(item);
            RefrescarGrids();
            ActualizarTotales();
        }

        // ─────────────────────────────────────────────────────────
        //  AGREGAR / ELIMINAR REPUESTOS
        // ─────────────────────────────────────────────────────────

        private void AgregarRepuesto_Click(object sender, RoutedEventArgs e)
        {
            var rep = dgRepuestosDisponibles.SelectedItem as Repuesto;
            if (rep == null)
            {
                MessageBox.Show("Seleccione un repuesto.", "Advertencia",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            if (!int.TryParse(txtCantidadRepuesto.Text, out int cant) || cant <= 0)
            {
                MessageBox.Show("Cantidad inválida.", "Advertencia",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            // Stock real disponible = StockActual en BD + lo que ya estaba asignado a este trabajo
            int yaAsignado = _repuestosTrabajo.Where(r => r.RepuestoID == rep.RepuestoID).Sum(r => r.Cantidad);
            int stockOrigen = _trabajo.Repuestos?
                .Where(r => r.RepuestoID == rep.RepuestoID)
                .Sum(r => r.Cantidad) ?? 0;
            int stockDisp = rep.StockActual + stockOrigen;  // stock real disponible para este trabajo

            if (yaAsignado + cant > stockDisp)
            {
                MessageBox.Show(
                    $"Stock insuficiente para '{rep.Nombre}'.\n\n" +
                    $"Disponible (real): {stockDisp}\n" +
                    $"Ya asignado:       {yaAsignado}\n" +
                    $"Solicitado:        {cant}",
                    "Stock Insuficiente",
                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            var existente = _repuestosTrabajo.FirstOrDefault(r => r.RepuestoID == rep.RepuestoID);
            if (existente != null)
            {
                existente.Cantidad += cant;
                existente.Subtotal = existente.Cantidad * existente.PrecioUnitario;
            }
            else
            {
                _repuestosTrabajo.Add(new RepuestoItem
                {
                    RepuestoID = rep.RepuestoID,
                    NombreRepuesto = rep.Nombre,
                    Cantidad = cant,
                    PrecioUnitario = rep.PrecioUnitario,
                    Subtotal = cant * rep.PrecioUnitario
                });
            }

            txtCantidadRepuesto.Text = "1";
            RefrescarGrids();
            ActualizarTotales();
        }

        private void EliminarRepuesto_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.DataContext as RepuestoItem;
            if (item == null) return;
            _repuestosTrabajo.Remove(item);
            RefrescarGrids();
            ActualizarTotales();
        }

        // ─────────────────────────────────────────────────────────
        //  GUARDAR  (sin finalizar)  ← bug de stock CORREGIDO
        // ─────────────────────────────────────────────────────────

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new TallerDbContext();

                var trabajo = db.Trabajos
                    .Include(t => t.Servicios)
                    .Include(t => t.Repuestos)
                    .FirstOrDefault(t => t.TrabajoID == _trabajoId);

                if (trabajo == null) return;

                // ╔══════════════════════════════════════════╗
                // ║  PASO 1 — Restaurar stock de repuestos   ║
                // ║  ANTERIORES antes de eliminarlos         ║
                // ╚══════════════════════════════════════════╝
                if (trabajo.Repuestos != null)
                    foreach (var tr in trabajo.Repuestos)
                    {
                        var rep = db.Repuestos.Find(tr.RepuestoID);
                        if (rep != null) rep.StockActual += tr.Cantidad;
                    }

                // PASO 2 — Eliminar relaciones anteriores
                db.Trabajos_Servicios.RemoveRange(trabajo.Servicios ?? Enumerable.Empty<Trabajos_Servicios>());
                db.Trabajos_Repuestos.RemoveRange(trabajo.Repuestos ?? Enumerable.Empty<Trabajos_Repuestos>());
                db.SaveChanges();

                // PASO 3 — Insertar servicios nuevos
                foreach (var s in _serviciosTrabajo)
                    db.Trabajos_Servicios.Add(new Trabajos_Servicios
                    {
                        TrabajoID = _trabajoId,
                        ServicioID = s.ServicioID,
                        Cantidad = s.Cantidad,
                        Subtotal = s.Subtotal
                    });

                // PASO 4 — Insertar repuestos nuevos y descontar stock
                foreach (var r in _repuestosTrabajo)
                {
                    db.Trabajos_Repuestos.Add(new Trabajos_Repuestos
                    {
                        TrabajoID = _trabajoId,
                        RepuestoID = r.RepuestoID,
                        Cantidad = r.Cantidad,
                        PrecioUnitario = r.PrecioUnitario,
                        Subtotal = r.Subtotal
                    });
                    var rep = db.Repuestos.Find(r.RepuestoID);
                    if (rep != null) rep.StockActual -= r.Cantidad;
                }

                // PASO 5 — Actualizar datos del trabajo
                // Estado
                var comboItem = cmbEstado.SelectedItem as ComboBoxItem;
                if (comboItem != null)
                    trabajo.Estado = comboItem.Tag?.ToString() ?? trabajo.Estado;

                // Descripción / notas
                if (!string.IsNullOrWhiteSpace(txtNotas.Text))
                    trabajo.Descripcion = txtNotas.Text.Trim();

                // Fecha de entrega
                if (dpFechaEntrega.SelectedDate.HasValue)
                    trabajo.FechaEntrega = dpFechaEntrega.SelectedDate.Value;

                // PrecioFinal: solo guardamos el precio calculado/manual como referencia
                // (NO se marca como finalizado aquí — eso lo hace FinalizarTrabajoWindow)
                trabajo.PrecioFinal = null;  // se limpia para que no aparezca antes de finalizar

                db.SaveChanges();

                MessageBox.Show(
                    $"✅ Cambios guardados correctamente.\n\n" +
                    $"Servicios: {_serviciosTrabajo.Count}\n" +
                    $"Repuestos: {_repuestosTrabajo.Count}\n\n" +
                    $"Subtotal actual: Bs. {Subtotal:N2}\n\n" +
                    $"Para cerrar la orden usa 'Finalizar Trabajo'.",
                    "Guardado", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  HELPERS UI
        // ─────────────────────────────────────────────────────────

        private void RefrescarGrids()
        {
            dgServiciosTrabajo.ItemsSource = null;
            dgServiciosTrabajo.ItemsSource = _serviciosTrabajo;
            dgRepuestosTrabajo.ItemsSource = null;
            dgRepuestosTrabajo.ItemsSource = _repuestosTrabajo;
        }

        private void ActualizarTotales()
        {
            lblTotalServicios.Text = $"Bs. {TotalServicios:N2}";
            lblTotalRepuestos.Text = $"Bs. {TotalRepuestos:N2}";
            lblSubtotal.Text = $"Bs. {Subtotal:N2}";
            lblTotalFooter.Text = $"Bs. {PrecioAGuardar:N2}";
            lblPrecioAGuardar.Text = $"Bs. {PrecioAGuardar:N2}";
        }

        private void SetComboEstado(string estado)
        {
            foreach (ComboBoxItem item in cmbEstado.Items)
                if (item.Tag?.ToString() == estado)
                { cmbEstado.SelectedItem = item; return; }
            cmbEstado.SelectedIndex = 0;
        }

        private void TxtBuscarServicio_TextChanged(object sender, TextChangedEventArgs e)
        {
            var f = txtBuscarServicio.Text.ToLower();
            dgServiciosDisponibles.ItemsSource = string.IsNullOrWhiteSpace(f)
                ? _todosLosServicios
                : _todosLosServicios.Where(s => s.Nombre.ToLower().Contains(f) ||
                    (s.Descripcion?.ToLower().Contains(f) ?? false)).ToList();
        }

        private void TxtBuscarRepuesto_TextChanged(object sender, TextChangedEventArgs e)
        {
            var f = txtBuscarRepuesto.Text.ToLower();
            dgRepuestosDisponibles.ItemsSource = string.IsNullOrWhiteSpace(f)
                ? _todosLosRepuestos
                : _todosLosRepuestos.Where(r => r.Nombre.ToLower().Contains(f) ||
                    (r.Descripcion?.ToLower().Contains(f) ?? false)).ToList();
        }

        private void TxtPrecioManual_TextChanged(object sender, TextChangedEventArgs e)
            => ActualizarTotales();

        private void CmbEstado_Changed(object sender, SelectionChangedEventArgs e)
        {
            // no acción adicional necesaria; se lee al guardar
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            var r = MessageBox.Show(
                "¿Salir sin guardar? Se perderán los cambios no guardados.",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r == MessageBoxResult.Yes) { DialogResult = false; Close(); }
        }
    }

    // ── DTOs para binding ─────────────────────────────────────────
    public class ServicioItem
    {
        public int ServicioID { get; set; }
        public string NombreServicio { get; set; }
        public int Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class RepuestoItem
    {
        public int RepuestoID { get; set; }
        public string NombreRepuesto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}