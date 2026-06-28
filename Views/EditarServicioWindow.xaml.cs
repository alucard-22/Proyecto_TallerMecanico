using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Proyecto_taller.Views
{
    public partial class EditarServicioWindow : Window
    {
        private readonly Servicio _servicio;
        private readonly bool _esNuevo;

        /// <summary>Editar servicio existente.</summary>
        public EditarServicioWindow(Servicio servicio)
        {
            InitializeComponent();
            _servicio = servicio;
            _esNuevo = false;

            txtTitulo.Text = $"✏️  Editar Servicio — {servicio.Nombre}";
            txtNombre.Text = servicio.Nombre;
            txtDescripcion.Text = servicio.Descripcion ?? "";
            txtCosto.Text = servicio.CostoBase.ToString("N2");

            foreach (ComboBoxItem item in cmbCategoria.Items)
                if (item.Content?.ToString() == servicio.Categoria)
                { cmbCategoria.SelectedItem = item; break; }

            if (cmbCategoria.SelectedIndex < 0) cmbCategoria.SelectedIndex = 0;

            SuscribirEventosValidacion();
            txtNombre.Focus();
        }

        /// <summary>Agregar nuevo servicio.</summary>
        public EditarServicioWindow()
        {
            InitializeComponent();
            _servicio = new Servicio();
            _esNuevo = true;

            txtTitulo.Text = "➕  Nuevo Servicio";
            cmbCategoria.SelectedIndex = 0;
            txtCosto.Text = "";

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

        // ── Guardar ───────────────────────────────────────────────────────────

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            txtNombre.Text = ValidationHelper.AplicarTitleCase(txtNombre.Text);
            if (!string.IsNullOrWhiteSpace(txtDescripcion.Text))
                txtDescripcion.Text = ValidationHelper.AplicarPrimeraLetraMayuscula(txtDescripcion.Text);

            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            { Msg("El nombre es obligatorio."); txtNombre.Focus(); return; }

            string costoStr = txtCosto.Text.Trim().Replace(",", ".");

            // FIX: antes se aceptaba costo = 0, lo cual crea un servicio
            // "gratis" sin sentido en el catálogo (un servicio siempre tiene
            // un costo base, aunque sea mínimo). Ahora se exige > 0.
            if (!decimal.TryParse(costoStr,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal costo) || costo <= 0)
            {
                Msg("El costo base debe ser un número válido mayor a 0.\n\n" +
                    "Un servicio no puede registrarse sin un costo asociado.");
                txtCosto.Focus();
                return;
            }

            try
            {
                using var db = new TallerDbContext();

                string categoria = (cmbCategoria.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Otro";

                if (_esNuevo)
                {
                    var nuevo = new Servicio
                    {
                        Nombre = txtNombre.Text.Trim(),
                        Descripcion = txtDescripcion.Text.Trim(),
                        Categoria = categoria,
                        CostoBase = costo
                    };
                    db.Servicios.Add(nuevo);
                }
                else
                {
                    var svc = db.Servicios.Find(_servicio.ServicioID);
                    if (svc == null) { Msg("No se encontró el servicio en la base de datos."); return; }

                    svc.Nombre = txtNombre.Text.Trim();
                    svc.Descripcion = txtDescripcion.Text.Trim();
                    svc.Categoria = categoria;
                    svc.CostoBase = costo;
                }

                db.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        { DialogResult = false; Close(); }

        private void Msg(string m)
            => MessageBox.Show(m, "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
