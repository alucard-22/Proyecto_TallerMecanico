using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using System;
using System.Windows;
using System.Windows.Input;

namespace Proyecto_taller.Views
{
    public partial class CambiarPasswordWindow : Window
    {
        private readonly int _usuarioId;

        private bool _nuevaPasswordVisible = false;
        private bool _confirmarPasswordVisible = false;

        public CambiarPasswordWindow(int usuarioId)
        {
            InitializeComponent();
            _usuarioId = usuarioId;

            using var db = new TallerDbContext();
            var usuario = db.Usuarios.Find(usuarioId);
            if (usuario != null)
                txtUsuarioInfo.Text =
                    $"{usuario.NombreUsuario}   ·   {usuario.NombreCompleto}";

            txtNuevaPassword.Focus();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        // ── NUEVO: mostrar/ocultar contraseña ─────────────────────────────────

        private void ToggleMostrarNuevaPassword_Click(object sender, RoutedEventArgs e)
        {
            _nuevaPasswordVisible = !_nuevaPasswordVisible;

            if (_nuevaPasswordVisible)
            {
                txtNuevaPasswordVisible.Text = txtNuevaPassword.Password;
                txtNuevaPassword.Visibility = Visibility.Collapsed;
                txtNuevaPasswordVisible.Visibility = Visibility.Visible;
                txtNuevaPasswordVisible.Focus();
                txtNuevaPasswordVisible.CaretIndex = txtNuevaPasswordVisible.Text.Length;
                btnToggleNuevaPassword.Content = "🙈";
            }
            else
            {
                txtNuevaPassword.Password = txtNuevaPasswordVisible.Text;
                txtNuevaPasswordVisible.Visibility = Visibility.Collapsed;
                txtNuevaPassword.Visibility = Visibility.Visible;
                txtNuevaPassword.Focus();
                btnToggleNuevaPassword.Content = "👁️";
            }
        }

        private void ToggleMostrarConfirmarPassword_Click(object sender, RoutedEventArgs e)
        {
            _confirmarPasswordVisible = !_confirmarPasswordVisible;

            if (_confirmarPasswordVisible)
            {
                txtConfirmarPasswordVisible.Text = txtConfirmarPassword.Password;
                txtConfirmarPassword.Visibility = Visibility.Collapsed;
                txtConfirmarPasswordVisible.Visibility = Visibility.Visible;
                txtConfirmarPasswordVisible.Focus();
                txtConfirmarPasswordVisible.CaretIndex = txtConfirmarPasswordVisible.Text.Length;
                btnToggleConfirmarPassword.Content = "🙈";
            }
            else
            {
                txtConfirmarPassword.Password = txtConfirmarPasswordVisible.Text;
                txtConfirmarPasswordVisible.Visibility = Visibility.Collapsed;
                txtConfirmarPassword.Visibility = Visibility.Visible;
                txtConfirmarPassword.Focus();
                btnToggleConfirmarPassword.Content = "👁️";
            }
        }

        private string ObtenerNuevaPassword()
            => _nuevaPasswordVisible ? txtNuevaPasswordVisible.Text : txtNuevaPassword.Password;

        private string ObtenerConfirmarPassword()
            => _confirmarPasswordVisible ? txtConfirmarPasswordVisible.Text : txtConfirmarPassword.Password;

        // ── Guardar ───────────────────────────────────────────────────────────

        private void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            string nueva = ObtenerNuevaPassword();
            string confirmar = ObtenerConfirmarPassword();

            if (string.IsNullOrWhiteSpace(nueva))
            {
                MessageBox.Show("Ingresa la nueva contraseña.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (nueva.Length < 6)
            {
                MessageBox.Show("La contraseña debe tener al menos 6 caracteres.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (nueva != confirmar)
            {
                MessageBox.Show("Las contraseñas no coinciden.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new TallerDbContext();
                var usuario = db.Usuarios.Find(_usuarioId);

                if (usuario != null)
                {
                    usuario.PasswordHash = PasswordHelper.HashPassword(nueva);
                    usuario.FechaUltimoCambioPassword = DateTime.Now;
                    db.SaveChanges();

                    MessageBox.Show(
                        "✅  Contraseña cambiada exitosamente.",
                        "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
