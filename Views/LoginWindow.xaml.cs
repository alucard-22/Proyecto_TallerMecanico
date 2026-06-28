using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using Proyecto_taller.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Proyecto_taller.Views
{
    public partial class LoginWindow : Window
    {
        // Indica si actualmente se está mostrando la contraseña en texto plano
        private bool _passwordVisible = false;

        public LoginWindow()
        {
            InitializeComponent();
            InicializarUsuariosPorDefecto();
            txtUsuario.Focus();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        // ── NUEVO: mostrar/ocultar contraseña ─────────────────────────────────
        // Alterna entre el PasswordBox (oculto, seguro) y un TextBox visible
        // sincronizado con el mismo contenido. Solo uno de los dos está visible
        // a la vez; el otro queda Collapsed para no interferir con el foco.
        private void ToggleMostrarPassword_Click(object sender, RoutedEventArgs e)
        {
            _passwordVisible = !_passwordVisible;

            if (_passwordVisible)
            {
                txtPasswordVisible.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPasswordVisible.Focus();
                txtPasswordVisible.CaretIndex = txtPasswordVisible.Text.Length;
                btnTogglePassword.Content = "🙈";
            }
            else
            {
                txtPassword.Password = txtPasswordVisible.Text;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                txtPassword.Focus();
                btnTogglePassword.Content = "👁️";
            }
        }

        // Devuelve el valor de la contraseña sin importar cuál de los dos
        // controles está visible en este momento.
        private string ObtenerPasswordActual()
            => _passwordVisible ? txtPasswordVisible.Text : txtPassword.Password;

        private void InicializarUsuariosPorDefecto()
        {
            try
            {
                using var db = new TallerDbContext();

                if (db.Usuarios.Any())
                {
                    var sinPermisos = db.Usuarios
                        .Where(u => u.PermisosJson == null || u.PermisosJson == "")
                        .ToList();

                    foreach (var u in sinPermisos)
                    {
                        u.Permisos = u.Rol == "Administrador"
                            ? SessionManager.ModulosDisponibles
                            : new List<string>
                            {
                                "Inicio", "Clientes", "Vehiculos",
                                "Trabajos", "Reservas", "Inventario",
                                "Recibos", "Reportes"
                            };
                    }

                    if (sinPermisos.Count > 0)
                        db.SaveChanges();

                    return;
                }

                db.Usuarios.AddRange(
                    new Usuario
                    {
                        NombreUsuario = "admin",
                        PasswordHash = PasswordHelper.HashPassword("admin123"),
                        NombreCompleto = "Administrador del Sistema",
                        Rol = "Administrador",
                        Activo = true,
                        FechaCreacion = DateTime.Now,
                        Permisos = SessionManager.ModulosDisponibles
                    },
                    new Usuario
                    {
                        NombreUsuario = "empleado",
                        PasswordHash = PasswordHelper.HashPassword("empleado123"),
                        NombreCompleto = "Empleado del Taller",
                        Rol = "Empleado",
                        Activo = true,
                        FechaCreacion = DateTime.Now,
                        Permisos = new List<string>
                        {
                            "Inicio", "Clientes", "Vehiculos",
                            "Trabajos", "Reservas", "Inventario",
                            "Recibos", "Reportes"
                        }
                    });

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al inicializar usuarios:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
            => IntentarIniciarSesion();

        private void TxtUsuario_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) txtPassword.Focus();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) IntentarIniciarSesion();
        }

        private void IntentarIniciarSesion()
        {
            OcultarError();

            if (string.IsNullOrWhiteSpace(txtUsuario.Text))
            {
                MostrarError("El campo de usuario está vacío.");
                txtUsuario.Focus();
                return;
            }

            string password = ObtenerPasswordActual();

            if (string.IsNullOrWhiteSpace(password))
            {
                MostrarError("El campo de contraseña está vacío.");
                return;
            }

            try
            {
                using var db = new TallerDbContext();

                var usuario = db.Usuarios.FirstOrDefault(u =>
                    u.NombreUsuario.ToLower() == txtUsuario.Text.Trim().ToLower());

                if (usuario == null)
                {
                    MostrarError(
                        $"El usuario \"{txtUsuario.Text.Trim()}\" no existe.\n\n" +
                        $"Usuarios de prueba: admin / empleado");
                    txtUsuario.SelectAll();
                    txtUsuario.Focus();
                    return;
                }

                if (!usuario.Activo)
                {
                    MostrarError(
                        $"La cuenta \"{usuario.NombreUsuario}\" está desactivada.\n\n" +
                        $"Contacta al administrador para reactivarla.");
                    return;
                }

                if (!PasswordHelper.VerifyPassword(password, usuario.PasswordHash))
                {
                    MostrarError(
                        "Contraseña incorrecta.\n\n" +
                        "Verifica que no tengas BLOQ MAYÚS activado.");
                    txtPassword.Clear();
                    txtPasswordVisible.Clear();
                    txtPassword.Focus();
                    return;
                }

                // Login exitoso
                usuario.UltimoAcceso = DateTime.Now;
                db.SaveChanges();

                SessionManager.IniciarSesion(usuario);

                // ── NUEVO: registrar el login en auditoría ──────────────────────
                AuditoriaHelper.Registrar(
                    "Login", "Usuario", usuario.UsuarioID,
                    $"Inicio de sesión: {usuario.NombreUsuario} ({usuario.Rol})");

                new MainWindow().Show();
                Close();
            }
            catch (Exception ex)
            {
                bool esConexion =
                    ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("server", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                    ex.InnerException?.Message.Contains("network", StringComparison.OrdinalIgnoreCase) == true;

                MostrarError(esConexion
                    ? "No se pudo conectar a la base de datos.\n\nVerifica que SQL Server esté activo."
                    : $"Error inesperado:\n{ex.Message}");
            }
        }

        private void MostrarError(string mensaje)
        {
            txtError.Text = mensaje;
            errorPanel.Visibility = Visibility.Visible;

            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
            errorPanel.BeginAnimation(OpacityProperty, fade);

            System.Media.SystemSounds.Hand.Play();
        }

        private void OcultarError()
        {
            errorPanel.Visibility = Visibility.Collapsed;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();
    }
}
