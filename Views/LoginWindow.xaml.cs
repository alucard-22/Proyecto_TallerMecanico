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
using Microsoft.EntityFrameworkCore;

namespace Proyecto_taller.Views
{
    /// <summary>
    /// Lógica de interacción para LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            InicializarUsuariosPorDefecto();
            txtUsuario.Focus();
        }

        /// <summary>
        /// Crea usuarios por defecto si no existen
        /// </summary>
        private void InicializarUsuariosPorDefecto()
        {
            try
            {
                using var db = new TallerDbContext();

                // Verificar si ya existen usuarios
                if (db.Usuarios.Any())
                    return;

                // Crear usuario Administrador
                var admin = new Models.Usuario
                {
                    NombreUsuario = "admin",
                    PasswordHash = PasswordHelper.HashPassword("admin123"),
                    NombreCompleto = "Administrador del Sistema",
                    Rol = "Administrador",
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                // Crear usuario Empleado
                var empleado = new Models.Usuario
                {
                    NombreUsuario = "empleado",
                    PasswordHash = PasswordHelper.HashPassword("empleado123"),
                    NombreCompleto = "Empleado del Taller",
                    Rol = "Empleado",
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                db.Usuarios.AddRange(admin, empleado);
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
        {
            IntentarIniciarSesion();
        }

        private void TxtUsuario_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                txtPassword.Focus();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                IntentarIniciarSesion();
        }

        private void IntentarIniciarSesion()
        {
            // Ocultar mensaje de error
            errorPanel.Visibility = Visibility.Collapsed;

            // Validar campos
            if (string.IsNullOrWhiteSpace(txtUsuario.Text))
            {
                MostrarError("Por favor ingrese su nombre de usuario.");
                txtUsuario.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MostrarError("Por favor ingrese su contraseña.");
                txtPassword.Focus();
                return;
            }

            try
            {
                using var db = new TallerDbContext();

                // Buscar usuario
                var usuario = db.Usuarios.FirstOrDefault(u =>
                    u.NombreUsuario.ToLower() == txtUsuario.Text.ToLower() &&
                    u.Activo);

                if (usuario == null)
                {
                    MostrarError("Usuario no encontrado o inactivo.");
                    return;
                }

                // Verificar contraseña
                if (!PasswordHelper.VerifyPassword(txtPassword.Password, usuario.PasswordHash))
                {
                    MostrarError("Contraseña incorrecta.");
                    return;
                }

                // Actualizar último acceso
                usuario.UltimoAcceso = DateTime.Now;
                db.SaveChanges();

                // Iniciar sesión
                SessionManager.IniciarSesion(usuario);

                // Mostrar mensaje de bienvenida
                MessageBox.Show(
                    $"✅ Bienvenido, {usuario.NombreCompleto}!\n\n" +
                    $"Rol: {usuario.Rol}\n" +
                    $"Último acceso: {(usuario.UltimoAcceso.HasValue ? usuario.UltimoAcceso.Value.ToString("dd/MM/yyyy HH:mm") : "Primer ingreso")}",
                    "Inicio de Sesión Exitoso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Abrir ventana principal
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                // Cerrar ventana de login
                this.Close();
            }
            catch (Exception ex)
            {
                MostrarError($"Error al iniciar sesión: {ex.Message}");
            }
        }

        private void MostrarError(string mensaje)
        {
            txtError.Text = mensaje;
            errorPanel.Visibility = Visibility.Visible;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}

