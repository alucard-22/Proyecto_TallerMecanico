using Proyecto_taller.Data;
using Proyecto_taller.Helpers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Proyecto_taller.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            InicializarUsuariosPorDefecto();
            txtUsuario.Focus();
        }

        // ─────────────────────────────────────────────────────────
        //  USUARIOS POR DEFECTO
        // ─────────────────────────────────────────────────────────

        private void InicializarUsuariosPorDefecto()
        {
            try
            {
                using var db = new TallerDbContext();
                if (db.Usuarios.Any()) return;

                db.Usuarios.AddRange(
                    new Models.Usuario
                    {
                        NombreUsuario = "admin",
                        PasswordHash = PasswordHelper.HashPassword("admin123"),
                        NombreCompleto = "Administrador del Sistema",
                        Rol = "Administrador",
                        Activo = true,
                        FechaCreacion = DateTime.Now
                    },
                    new Models.Usuario
                    {
                        NombreUsuario = "empleado",
                        PasswordHash = PasswordHelper.HashPassword("empleado123"),
                        NombreCompleto = "Empleado del Taller",
                        Rol = "Empleado",
                        Activo = true,
                        FechaCreacion = DateTime.Now
                    });

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar usuarios:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  EVENTOS
        // ─────────────────────────────────────────────────────────

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

        // ─────────────────────────────────────────────────────────
        //  LÓGICA DE LOGIN
        // ─────────────────────────────────────────────────────────

        private void IntentarIniciarSesion()
        {
            OcultarError();

            if (string.IsNullOrWhiteSpace(txtUsuario.Text))
            {
                MostrarError("El campo de usuario está vacío.");
                txtUsuario.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MostrarError("El campo de contraseña está vacío.");
                txtPassword.Focus();
                return;
            }

            try
            {
                using var db = new TallerDbContext();

                var usuario = db.Usuarios.FirstOrDefault(u =>
                    u.NombreUsuario.ToLower() == txtUsuario.Text.Trim().ToLower());

                // Usuario no existe
                if (usuario == null)
                {
                    MostrarError(
                        $"El usuario \"{txtUsuario.Text.Trim()}\" no existe.\n\n" +
                        $"Usuarios de prueba: admin / empleado");
                    txtUsuario.SelectAll();
                    txtUsuario.Focus();
                    return;
                }

                // Usuario inactivo
                if (!usuario.Activo)
                {
                    MostrarError(
                        $"La cuenta \"{usuario.NombreUsuario}\" está desactivada.\n\n" +
                        $"Contacta al administrador para reactivarla.");
                    return;
                }

                // Contraseña incorrecta
                if (!PasswordHelper.VerifyPassword(txtPassword.Password, usuario.PasswordHash))
                {
                    MostrarError(
                        "Contraseña incorrecta.\n\n" +
                        "Verifica que no tengas BLOQ MAYÚS activado.");
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                // ✅ Login exitoso
                usuario.UltimoAcceso = DateTime.Now;
                db.SaveChanges();

                SessionManager.IniciarSesion(usuario);

                MessageBox.Show(
                    $"✅  Bienvenido, {usuario.NombreCompleto}\n\n" +
                    $"Rol: {usuario.Rol}\n" +
                    $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}",
                    "Inicio de Sesión",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                new MainWindow().Show();
                Close();
            }
            catch (Exception ex)
            {
                // Detectar error de conexión
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

        // ─────────────────────────────────────────────────────────
        //  PANEL DE ERROR
        // ─────────────────────────────────────────────────────────

        private void MostrarError(string mensaje)
        {
            txtError.Text = mensaje;
            errorPanel.Visibility = Visibility.Visible;

            // Animación de aparición suave
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
            errorPanel.BeginAnimation(OpacityProperty, fade);

            System.Media.SystemSounds.Hand.Play();
        }

        private void OcultarError()
        {
            errorPanel.Visibility = Visibility.Collapsed;
        }

        // ─────────────────────────────────────────────────────────
        //  CERRAR
        // ─────────────────────────────────────────────────────────

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();
    }
}