using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
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
    public partial class CambiarPasswordWindow : Window
    {
        private int _usuarioId;

        public CambiarPasswordWindow(int usuarioId)
        {
            InitializeComponent();
            _usuarioId = usuarioId;

            using var db = new TallerDbContext();
            var usuario = db.Usuarios.Find(usuarioId);
            if (usuario != null)
            {
                txtUsuarioInfo.Text = $"Usuario: {usuario.NombreUsuario} - {usuario.NombreCompleto}";
            }

            txtNuevaPassword.Focus();
        }

        private void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNuevaPassword.Password))
            {
                MessageBox.Show("Ingrese la nueva contraseña.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (txtNuevaPassword.Password.Length < 6)
            {
                MessageBox.Show("La contraseña debe tener al menos 6 caracteres.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (txtNuevaPassword.Password != txtConfirmarPassword.Password)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new TallerDbContext();
                var usuario = db.Usuarios.Find(_usuarioId);

                if (usuario != null)
                {
                    usuario.PasswordHash = PasswordHelper.HashPassword(txtNuevaPassword.Password);
                    db.SaveChanges();

                    MessageBox.Show(
                        "✅ Contraseña cambiada exitosamente.",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
