using Proyecto_taller.Data;
using Proyecto_taller.Models;
using System;
using System.Windows;
using System.Windows.Input;
using Proyecto_taller.Helpers;

namespace Proyecto_taller.Views
{
    public partial class EditarRepuestoWindow : Window
    {
        private readonly Repuesto? _repuesto;
        private readonly bool _esNuevo;

        // ── Modo EDITAR ───────────────────────────────────────────────────────
        public EditarRepuestoWindow(Repuesto repuesto)
        {
            InitializeComponent();
            _repuesto = repuesto;
            _esNuevo = false;

            txtTitulo.Text = $"✏️  Editar Repuesto — {repuesto.Nombre}";
            txtNombre.Text = repuesto.Nombre;
            txtDescripcion.Text = repuesto.Descripcion ?? string.Empty;
            txtPrecio.Text = repuesto.PrecioUnitario.ToString("N2");
            txtStockMinimo.Text = repuesto.StockMinimo.ToString();

            panelStockInicial.Visibility = System.Windows.Visibility.Collapsed;

            SuscribirEventosValidacion();
            txtNombre.Focus();
        }

        // ── Modo NUEVO ────────────────────────────────────────────────────────
        public EditarRepuestoWindow()
        {
            InitializeComponent();
            _repuesto = null;
            _esNuevo = true;

            txtTitulo.Text = "➕  Nuevo Repuesto";
            txtPrecio.Text = "";
            txtStockMinimo.Text = "5";
            txtStockInicial.Text = "0";

            panelStockInicial.Visibility = System.Windows.Visibility.Visible;

            SuscribirEventosValidacion();
            txtNombre.Focus();
        }

        private void SuscribirEventosValidacion()
        {
            txtNombre.LostFocus += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(txtNombre.Text))
                    txtNombre.Text = ValidationHelper.AplicarTitleCase(txtNombre.Text);
            };

            txtDescripcion.LostFocus += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(txtDescripcion.Text))
                    txtDescripcion.Text = ValidationHelper.AplicarPrimeraLetraMayuscula(txtDescripcion.Text);
            };
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        // ── Guardar ───────────────────────────────────────────────────────────

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            txtNombre.Text = ValidationHelper.AplicarTitleCase(txtNombre.Text);
            if (!string.IsNullOrWhiteSpace(txtDescripcion.Text))
                txtDescripcion.Text = ValidationHelper.AplicarPrimeraLetraMayuscula(txtDescripcion.Text);

            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                Msg("El nombre del repuesto es obligatorio.");
                txtNombre.Focus();
                return;
            }

            string precioStr = txtPrecio.Text.Trim().Replace(",", ".");

            // FIX: antes se aceptaba precio = 0, lo cual no tiene sentido para
            // un repuesto real en inventario (genera un "valor total" de 0 en
            // los reportes y distorsiona el cálculo de inventario). Ahora > 0.
            if (!decimal.TryParse(precioStr,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal precio) || precio <= 0)
            {
                Msg("El precio unitario debe ser un número válido mayor a 0.\n\n" +
                    "Un repuesto no puede registrarse sin un precio asociado.");
                txtPrecio.Focus();
                return;
            }

            if (!int.TryParse(txtStockMinimo.Text.Trim(), out int stockMin) || stockMin < 0)
            {
                Msg("El stock mínimo debe ser un número entero mayor o igual a 0.");
                txtStockMinimo.Focus();
                return;
            }

            int stockInicial = 0;
            if (_esNuevo)
            {
                if (!int.TryParse(txtStockInicial.Text.Trim(), out stockInicial) || stockInicial < 0)
                {
                    Msg("El stock inicial debe ser un número entero mayor o igual a 0.");
                    txtStockInicial.Focus();
                    return;
                }
            }

            try
            {
                using var db = new TallerDbContext();

                if (_esNuevo)
                {
                    if (db.Repuestos.Any(r =>
                            r.Nombre.ToLower() == txtNombre.Text.Trim().ToLower()))
                    {
                        Msg($"Ya existe un repuesto con el nombre '{txtNombre.Text.Trim()}'.");
                        txtNombre.Focus();
                        return;
                    }

                    var nuevo = new Repuesto
                    {
                        Nombre = txtNombre.Text.Trim(),
                        Descripcion = txtDescripcion.Text.Trim(),
                        PrecioUnitario = precio,
                        StockActual = stockInicial,
                        StockMinimo = stockMin,
                        FechaRegistro = DateTime.Now
                    };

                    db.Repuestos.Add(nuevo);
                    db.SaveChanges();

                    MessageBox.Show(
                        $"✅  Repuesto creado correctamente.\n\n" +
                        $"Nombre:         {nuevo.Nombre}\n" +
                        $"Precio:         Bs. {nuevo.PrecioUnitario:N2}\n" +
                        $"Stock inicial:  {nuevo.StockActual}\n" +
                        $"Stock mínimo:   {nuevo.StockMinimo}",
                        "Repuesto Creado",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var rep = db.Repuestos.Find(_repuesto!.RepuestoID);
                    if (rep == null)
                    {
                        Msg("No se encontró el repuesto en la base de datos.");
                        return;
                    }

                    if (db.Repuestos.Any(r =>
                            r.Nombre.ToLower() == txtNombre.Text.Trim().ToLower() &&
                            r.RepuestoID != rep.RepuestoID))
                    {
                        Msg($"Ya existe otro repuesto con el nombre '{txtNombre.Text.Trim()}'.");
                        txtNombre.Focus();
                        return;
                    }

                    rep.Nombre = txtNombre.Text.Trim();
                    rep.Descripcion = txtDescripcion.Text.Trim();
                    rep.PrecioUnitario = precio;
                    rep.StockMinimo = stockMin;

                    db.SaveChanges();

                    MessageBox.Show(
                        $"✅  Repuesto actualizado correctamente.\n\n" +
                        $"Nombre:       {rep.Nombre}\n" +
                        $"Precio:       Bs. {rep.PrecioUnitario:N2}\n" +
                        $"Stock mínimo: {rep.StockMinimo}",
                        "Repuesto Actualizado",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Msg(string m)
            => MessageBox.Show(m, "Validación",
                MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
