using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
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
    /// <summary>
    /// Lógica de interacción para AgregarUsuarioWindow.xaml
    /// </summary>
    public partial class AgregarUsuarioWindow : Window
    {
        public AgregarUsuarioWindow()
        {
            InitializeComponent();
            txtNombreUsuario.Focus();
        }

        private void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(txtNombreUsuario.Text))
            {
                MessageBox.Show("El nombre de usuario es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombreUsuario.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNombreCompleto.Text))
            {
                MessageBox.Show("El nombre completo es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombreCompleto.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("La contraseña es obligatoria.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return;
            }

            if (txtPassword.Password.Length < 6)
            {
                MessageBox.Show("La contraseña debe tener al menos 6 caracteres.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return;
            }

            if (txtPassword.Password != txtConfirmarPassword.Password)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtConfirmarPassword.Focus();
                return;
            }

            try
            {
                using var db = new TallerDbContext();

                // Verificar si el usuario ya existe
                var usuarioExiste = db.Usuarios.Any(u =>
                    u.NombreUsuario.ToLower() == txtNombreUsuario.Text.ToLower());

                if (usuarioExiste)
                {
                    MessageBox.Show(
                        "Ya existe un usuario con ese nombre de usuario.",
                        "Usuario Duplicado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    txtNombreUsuario.Focus();
                    return;
                }

                // Crear nuevo usuario
                var nuevoUsuario = new Usuario
                {
                    NombreUsuario = txtNombreUsuario.Text.Trim(),
                    NombreCompleto = txtNombreCompleto.Text.Trim(),
                    PasswordHash = PasswordHelper.HashPassword(txtPassword.Password),
                    Rol = ((ComboBoxItem)cmbRol.SelectedItem).Content.ToString(),
                    Activo = chkActivo.IsChecked == true,
                    FechaCreacion = DateTime.Now
                };

                db.Usuarios.Add(nuevoUsuario);
                db.SaveChanges();

                MessageBox.Show(
                    $"✅ Usuario '{nuevoUsuario.NombreUsuario}' creado exitosamente.\n\n" +
                    $"Rol: {nuevoUsuario.Rol}\n" +
                    $"Estado: {(nuevoUsuario.Activo ? "Activo" : "Inactivo")}",
                    "Usuario Creado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al crear el usuario:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
