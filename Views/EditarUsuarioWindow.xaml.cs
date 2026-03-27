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
    public partial class EditarUsuarioWindow : Window
    {
        private readonly Usuario _usuario;

        public EditarUsuarioWindow(Usuario usuario)
        {
            InitializeComponent();
            _usuario = usuario;

            txtTitulo.Text = $"✏️  Editar — {usuario.NombreUsuario}";
            txtSubtitulo.Text = $"Puedes cambiar el nombre completo y el rol.";
            txtNombreUsuario.Text = usuario.NombreUsuario;
            txtNombreCompleto.Text = usuario.NombreCompleto;

            // Preseleccionar el rol actual
            foreach (ComboBoxItem item in cmbRol.Items)
                if (item.Tag?.ToString() == usuario.Rol)
                { cmbRol.SelectedItem = item; break; }

            if (cmbRol.SelectedIndex < 0) cmbRol.SelectedIndex = 1;
            txtNombreCompleto.Focus();
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombreCompleto.Text))
            {
                MessageBox.Show("El nombre completo es obligatorio.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombreCompleto.Focus();
                return;
            }

            string rolSeleccionado =
                (cmbRol.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Empleado";

            try
            {
                using var db = new TallerDbContext();
                var usuario = db.Usuarios.Find(_usuario.UsuarioID);
                if (usuario == null)
                {
                    MessageBox.Show("El usuario ya no existe en la base de datos.",
                        "No encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                usuario.NombreCompleto = txtNombreCompleto.Text.Trim();
                usuario.Rol = rolSeleccionado;
                db.SaveChanges();

                MessageBox.Show(
                    $"✅  Usuario actualizado correctamente.\n\n" +
                    $"Nombre: {usuario.NombreCompleto}\n" +
                    $"Rol:    {usuario.Rol}",
                    "Guardado", MessageBoxButton.OK, MessageBoxImage.Information);

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
    }
}
