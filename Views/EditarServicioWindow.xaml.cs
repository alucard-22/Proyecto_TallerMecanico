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

            // Seleccionar categoría
            foreach (ComboBoxItem item in cmbCategoria.Items)
                if (item.Content?.ToString() == servicio.Categoria)
                { cmbCategoria.SelectedItem = item; break; }

            if (cmbCategoria.SelectedIndex < 0) cmbCategoria.SelectedIndex = 0;
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
            txtCosto.Text = "0.00";
            txtNombre.Focus();
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            { Msg("El nombre es obligatorio."); return; }

            if (!decimal.TryParse(txtCosto.Text.Trim(), out decimal costo) || costo < 0)
            { Msg("El costo base debe ser un número válido mayor o igual a 0."); return; }

            try
            {
                using var db = new TallerDbContext();

                if (_esNuevo)
                {
                    var nuevo = new Servicio
                    {
                        Nombre = txtNombre.Text.Trim(),
                        Descripcion = txtDescripcion.Text.Trim(),
                        Categoria = (cmbCategoria.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Otro",
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
                    svc.Categoria = (cmbCategoria.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Otro";
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
